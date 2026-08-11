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
    private readonly ITenantActual _tenantActual;
    private readonly IUsuarioActual _usuario;

    public SolicitudService(
        IAlumnoService alumnos, IAlumnoRepository alumnoRepo, ISolicitudCupoRepository pedidos,
        ITenantRepository tenants, ITenantActual tenantActual, IUsuarioActual usuario)
    {
        _alumnos = alumnos;
        _alumnoRepo = alumnoRepo;
        _pedidos = pedidos;
        _tenants = tenants;
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
    /// La lista de espera del club, derivada de dos motivos:
    /// <list type="bullet">
    /// <item>los ACTIVOS sin ninguna clase asignada (el pausado y el de baja no están
    /// esperando nada, así que no entran);</item>
    /// <item>los que tienen un pedido de cupo sin resolver — tengan clase o no. Es el
    /// caso que pone a alguien en Alumnos y en la espera al mismo tiempo.</item>
    /// </list>
    /// Alguien sin clase Y con un pedido aparece una sola vez, como pedido: es la fila
    /// que le da al profe algo para hacer.
    /// </summary>
    public async Task<IReadOnlyList<SolicitudPendienteDto>> PendientesAsync(
        CancellationToken ct = default)
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

        var filas = pedidos.Select(p => Fila(
            mias[p.AlumnoId],
            MotivoEspera.PidioCupo,
            p.Id,
            p.Horario is { } h ? HorarioService.TituloDe(h.Nombre, h.Alumnos) : null));

        var sinClase = mias.Values
            .Where(a => !conClase.Contains(a.Id) && !conPedido.Contains(a.Id))
            .Select(a => Fila(a, MotivoEspera.SinClase, null, null));

        // Tercer motivo: el que YA es alumno y el profe anotó a mano. Al que no tiene
        // ninguna clase la marca no le agrega nada (ya está arriba, en sinClase) y
        // gana ese motivo, que es el que ofrece sacarlo de la academia.
        var anotados = mias.Values
            .Where(a => a.EnEsperaDesde is not null && conClase.Contains(a.Id) && !conPedido.Contains(a.Id))
            .Select(a => Fila(a, MotivoEspera.LoAnotoElProfe, null, null));

        return [.. filas.Concat(sinClase).Concat(anotados).OrderBy(f => f.CreadoEl)];
    }

    public async Task<int> ContarPendientesAsync(CancellationToken ct = default) =>
        // Cuenta lo mismo que muestra la lista: son dos queries, no vale la pena un
        // atajo que después se desincronice del criterio de arriba.
        (await PendientesAsync(ct)).Count;

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

    private static SolicitudPendienteDto Fila(
        Alumno a, MotivoEspera motivo, Guid? solicitudId, string? clase) => new()
    {
        Id = a.Id, // el id de la FICHA (el del pedido va aparte, en SolicitudId)
        Nombre = a.Nombre,
        Apellido = a.Apellido,
        Email = a.Email ?? string.Empty,
        Dni = a.Dni,
        Telefono = a.Telefono,
        FechaNacimiento = a.FechaNacimiento,
        EsMenor = a.EsMenor,
        Categoria = a.Categoria.ToString(),
        Mensaje = a.Notas,
        // Desde cuándo espera, que es lo que ordena la lista. Para el anotado a mano
        // es cuándo lo anotaste: su ficha puede ser de hace dos años y recién ahora
        // haberte pedido otra clase.
        CreadoEl = motivo == MotivoEspera.LoAnotoElProfe ? a.EnEsperaDesde ?? a.CreadoEl : a.CreadoEl,
        Motivo = motivo.ToString(),
        SolicitudId = solicitudId,
        Clase = clase,
    };
}
