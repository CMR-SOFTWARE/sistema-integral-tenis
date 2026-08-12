using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Lista de espera (antes "solicitudes"): quién está esperando una clase. Ya no es
/// un estado de la ficha —eso hacía que estar en la espera te sacara de Alumnos—,
/// sino una consulta que junta dos motivos: el que no tiene ninguna clase y el que
/// pidió sumarse a una y el profe no resolvió todavía.
/// </summary>
public class SolicitudService : ISolicitudService
{
    private readonly IAlumnoService _alumnos;
    private readonly IAlumnoRepository _alumnoRepo;
    private readonly ISolicitudCupoRepository _pedidos;
    private readonly ITenantRepository _tenants;
    private readonly IMembresiaTenantRepository _membresias;
    private readonly ITenantActual _tenantActual;
    private readonly IUsuarioActual _usuario;

    public SolicitudService(
        IAlumnoService alumnos, IAlumnoRepository alumnoRepo, ISolicitudCupoRepository pedidos,
        ITenantRepository tenants, IMembresiaTenantRepository membresias,
        ITenantActual tenantActual, IUsuarioActual usuario)
    {
        _alumnos = alumnos;
        _alumnoRepo = alumnoRepo;
        _pedidos = pedidos;
        _tenants = tenants;
        _membresias = membresias;
        _tenantActual = tenantActual;
        _usuario = usuario;
    }

    public async Task<IReadOnlyList<MiSolicitudDto>> CrearAsync(
        Usuario usuario, CrearSolicitudDto dto, CancellationToken ct = default)
    {
        var club = await _tenants.ObtenerPorIdAsync(dto.TenantId, ct);
        if (club is null || club.Estado != EstadoTenant.Activo)
            throw new ReglaDeNegocioException("Ese club no existe o no está disponible.");

        // Un club por persona POR AHORA (identidad global, sin scopear por tenant)
        if (await _alumnoRepo.ObtenerPorUserIdAsync(usuario.Id, ct) is not null)
            throw new ReglaDeNegocioException(
                "Ya estás vinculado a un club. Por ahora se puede pertenecer a uno solo.");

        // La ficha se arma con TUS datos: tienen que estar completos
        if (string.IsNullOrWhiteSpace(usuario.Dni) ||
            string.IsNullOrWhiteSpace(usuario.PhoneNumber) ||
            usuario.FechaNacimiento is null)
            throw new ReglaDeNegocioException(
                "Completá tu DNI, teléfono y fecha de nacimiento antes de unirte (Mi perfil).");

        // Unirse = crear la ficha en ese club (sin aprobar). Queda esperando porque
        // todavía no tiene clase; se vuelve alumno cuando el profe le asigna una.
        // El mensaje opcional queda como nota.
        _tenantActual.Establecer(dto.TenantId);
        await _alumnos.CrearVinculadoAsync(new CreateAlumnoDto
        {
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Dni = usuario.Dni,
            Telefono = usuario.PhoneNumber ?? string.Empty, // validado no-vacío arriba
            Email = usuario.Email ?? string.Empty,
            FechaNacimiento = usuario.FechaNacimiento.Value,
            Categoria = usuario.Categoria ?? CategoriaAlumno.SinCategoria,
            ConsentimientoDatos = true, // lo dio él mismo al registrarse
            Notas = string.IsNullOrWhiteSpace(dto.Mensaje) ? null : dto.Mensaje.Trim(),
        }, usuario.Id, ct);

        return await MisAsync(usuario.Id, ct);
    }

    // Ya no hay solicitudes con estado: unirse deja la ficha en espera directo.
    // El portal se entera por su sesión (la ficha viene marcada como en espera).
    public Task<IReadOnlyList<MiSolicitudDto>> MisAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MiSolicitudDto>>([]);

    /// <summary>
    /// Quién espera y por qué, sin los datos de la ficha. Es la regla del negocio pura, y
    /// vive aparte del mapeo porque el badge de la pestaña solo necesita CONTAR: si
    /// contar arrastrara el armado de las filas (deuda, foto, club) le pagaríamos tres
    /// consultas de más a una pantalla que se abre todo el tiempo.
    ///
    /// Los motivos, por prioridad:
    /// <list type="bullet">
    /// <item><b>PidioCupo</b>: pidió sumarse a una clase y el profe no resolvió — tenga
    /// clase o no. Es el caso que pone a alguien en Alumnos y en la espera a la vez.</item>
    /// <item><b>SinClase</b>: activo y sin ninguna clase asignada (el pausado y el de baja
    /// no están esperando nada, así que no entran).</item>
    /// <item><b>LoAnotoElProfe</b>: ya es alumno y el profe lo anotó a mano.</item>
    /// </list>
    /// Una fila por persona: gana el motivo más concreto.
    /// </summary>
    private async Task<IReadOnlyList<EnEspera>> IdsEnEsperaAsync(CancellationToken ct)
    {
        var conClase = await _alumnoRepo.ListarConClaseAsync(ct);
        IEnumerable<Alumno> fichas = await _alumnoRepo.ListarAsync(null, EstadoAlumno.Activo, ct);

        // El profe EMPLEADO ve solo SU lista de espera (los que él cargó); el dueño, toda.
        if (_usuario.EsStaff)
            fichas = fichas.Where(a => a.ProfesorUserId == _usuario.UserId);
        var mias = fichas.ToDictionary(a => a.Id);

        var pedidos = (await _pedidos.ListarPorEstadoAsync(EstadoSolicitudGrupo.Pendiente, ct))
            .Where(p => mias.ContainsKey(p.AlumnoId))
            .ToList();
        var conPedido = pedidos.Select(p => p.AlumnoId).ToHashSet();

        var filas = pedidos.Select(p => new EnEspera(
            mias[p.AlumnoId].Id,
            MotivoEspera.PidioCupo,
            p.Id,
            p.Horario is { } h ? HorarioService.TituloDe(h.Nombre, h.Alumnos) : null,
            mias[p.AlumnoId].CreadoEl));

        // El profe (director o empleado) que además se dio de alta para tener su ficha no
        // está esperando una clase: está trabajando. Sin esto, el director que se anota
        // para ponerse su categoría aparece como si le faltara horario.
        var deProfes = await UserIdsDeProfesAsync(ct);
        var sinClase = mias.Values
            .Where(a => !conClase.Contains(a.Id) && !conPedido.Contains(a.Id))
            .Where(a => a.UserId is not { } uid || !deProfes.Contains(uid))
            .Select(a => new EnEspera(a.Id, MotivoEspera.SinClase, null, null, a.CreadoEl));

        // Tercer motivo: el que YA es alumno y el profe anotó a mano. Al que no tiene
        // ninguna clase la marca no le agrega nada (ya está arriba, en sinClase) y
        // gana ese motivo, que es el que ofrece sacarlo de la academia.
        //
        // Acá el profe SÍ entra: si alguien lo anotó a mano es porque quiere clase.
        var anotados = mias.Values
            .Where(a => a.EnEsperaDesde is not null && conClase.Contains(a.Id) && !conPedido.Contains(a.Id))
            .Select(a => new EnEspera(
                a.Id, MotivoEspera.LoAnotoElProfe, null, null, a.EnEsperaDesde ?? a.CreadoEl));

        return [.. filas.Concat(sinClase).Concat(anotados).OrderBy(f => f.EsperaDesde)];
    }

    /// <summary>
    /// Los usuarios que trabajan en el club: el dueño y los empleados ACTIVOS. El
    /// empleado dado de baja vuelve a ser una persona común, así que si no tiene clase
    /// espera como cualquiera.
    /// </summary>
    private async Task<HashSet<Guid>> UserIdsDeProfesAsync(CancellationToken ct)
    {
        var tenant = await _tenants.ObtenerActualAsync(ct);
        var profes = (await _membresias.ListarConUsuarioAsync(ct))
            .Where(x => x.Membresia.Activo)
            .Select(x => x.Membresia.UserId)
            .ToHashSet();

        if (tenant.OwnerUserId is { } ownerId) profes.Add(ownerId);
        return profes;
    }

    public async Task<IReadOnlyList<EsperaResponseDto>> PendientesAsync(
        CancellationToken ct = default)
    {
        var espera = await IdsEnEsperaAsync(ct);
        if (espera.Count == 0) return [];

        // La ficha se mapea con el MISMO camino que la pestaña Alumnos: así las dos
        // tablas muestran lo mismo (club, profe, cuota, estado) sin duplicar el mapeo.
        var fichas = (await _alumnos.ListarPorIdsAsync([.. espera.Select(e => e.AlumnoId)], ct))
            .ToDictionary(a => a.Id);

        return [.. espera
            .Where(e => fichas.ContainsKey(e.AlumnoId))
            .Select(e => Fila(fichas[e.AlumnoId], e))];
    }

    public async Task<int> ContarPendientesAsync(CancellationToken ct = default) =>
        // Mismo criterio que la lista, sin el costo de armar las filas.
        (await IdsEnEsperaAsync(ct)).Count;

    /// <summary>
    /// Saca de la academia al que espera SIN clase: borrado real de la ficha. Al que
    /// espera por un pedido no se le toca la ficha — se le rechaza el pedido desde
    /// <see cref="ISolicitudCupoService.RechazarAsync"/>.
    /// </summary>
    public async Task QuitarDeEsperaAsync(Guid alumnoId, CancellationToken ct = default)
    {
        var ficha = await _alumnoRepo.ObtenerAsync(alumnoId, ct)
            ?? throw new ReglaDeNegocioException("Esa ficha no existe.");

        // El staff solo quita de SU lista de espera.
        if (_usuario.EsStaff && ficha.ProfesorUserId != _usuario.UserId)
            throw new ReglaDeNegocioException("Ese registro no es de tu lista de espera.");

        // Guard: el que YA tiene clase es un alumno, y esto borra la ficha entera. Si
        // el profe lo quiere sacar, va por la baja o el borrado desde su ficha, donde
        // la confirmación dice lo que se pierde.
        var conClase = await _alumnoRepo.FiltrarConClaseAsync([alumnoId], ct);
        if (conClase.Contains(alumnoId))
        {
            // Salvo que esté ahí porque vos lo anotaste: entonces "sacarlo de la
            // espera" es apagar esa marca, y sigue siendo alumno como hasta recién.
            if (ficha.EnEsperaDesde is not null)
            {
                ficha.EnEsperaDesde = null;
                ficha.ActualizadoEl = DateTime.UtcNow;
                await _alumnoRepo.GuardarCambiosAsync(ct);
                return;
            }

            throw new ReglaDeNegocioException(
                $"{ficha.Nombre} {ficha.Apellido} ya tiene clase asignada: sacalo desde su ficha.");
        }

        // Borra la ficha y su historial. El Usuario NO se toca: la persona puede
        // unirse a otro club o volver a intentar.
        await _alumnoRepo.EliminarDefinitivoAsync(ficha, ct);
    }

    public async Task CambiarEsperaAsync(Guid alumnoId, bool enEspera, CancellationToken ct = default)
    {
        var ficha = await _alumnoRepo.ObtenerAsync(alumnoId, ct)
            ?? throw new ReglaDeNegocioException("Esa ficha no existe.");

        // Mismo criterio que el resto: el staff solo toca a SUS alumnos.
        if (_usuario.EsStaff && ficha.ProfesorUserId != _usuario.UserId)
            throw new ReglaDeNegocioException("Ese alumno no es tuyo.");

        // Marcar dos veces no pisa la fecha: si la pisáramos, volver a tocar el botón
        // lo mandaría al final de la cola cuando en realidad hace rato que espera.
        if (enEspera)
            ficha.EnEsperaDesde ??= DateTime.UtcNow;
        else
            ficha.EnEsperaDesde = null;

        ficha.ActualizadoEl = DateTime.UtcNow;
        await _alumnoRepo.GuardarCambiosAsync(ct);
    }

    /// <summary>
    /// Quién espera, por qué y desde cuándo — sin los datos de la ficha, que se resuelven
    /// después en una sola pasada.
    /// </summary>
    private readonly record struct EnEspera(
        Guid AlumnoId, MotivoEspera Motivo, Guid? SolicitudId, string? Clase, DateTime EsperaDesde);

    /// <summary>La ficha (mapeada como en Alumnos) más lo propio de la espera.</summary>
    private static EsperaResponseDto Fila(AlumnoResponseDto a, EnEspera e) => new()
    {
        // Los datos de la ficha se copian del DTO que ya armó AlumnoService: si mañana
        // gana un campo, se agrega en un solo lugar y las dos tablas lo muestran.
        Id = a.Id,
        Nombre = a.Nombre,
        Apellido = a.Apellido,
        Dni = a.Dni,
        Telefono = a.Telefono,
        Email = a.Email,
        FechaNacimiento = a.FechaNacimiento,
        EsMenor = a.EsMenor,
        Categoria = a.Categoria,
        Estado = a.Estado,
        Modalidad = a.Modalidad,
        Arancel = a.Arancel,
        Notas = a.Notas,
        TutorId = a.TutorId,
        CreadoEl = a.CreadoEl,
        DeudaVencida = a.DeudaVencida,
        TieneUsuario = a.TieneUsuario,
        FamiliaId = a.FamiliaId,
        FotoUrl = a.FotoUrl,
        ProfesorUserId = a.ProfesorUserId,
        SedeId = a.SedeId,
        SedeNombre = a.SedeNombre,
        TieneClase = a.TieneClase,
        EnEspera = a.EnEspera,
        // Lo propio de la espera
        Motivo = e.Motivo.ToString(),
        SolicitudId = e.SolicitudId,
        Clase = e.Clase,
        EsperaDesde = e.EsperaDesde,
    };
}
