using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Lista de espera (antes "solicitudes"): el jugador se UNE a un club y su ficha
/// nace directo en espera (<see cref="EstadoAlumno.EnEspera"/>), sin aprobación.
/// El profe la ve, la puede quitar, y se vuelve alumno cuando le asigna una clase.
/// </summary>
public class SolicitudService : ISolicitudService
{
    private readonly IAlumnoService _alumnos;
    private readonly IAlumnoRepository _alumnoRepo;
    private readonly ITenantRepository _tenants;
    private readonly ITenantActual _tenantActual;
    private readonly IUsuarioActual _usuario;

    public SolicitudService(
        IAlumnoService alumnos, IAlumnoRepository alumnoRepo,
        ITenantRepository tenants, ITenantActual tenantActual, IUsuarioActual usuario)
    {
        _alumnos = alumnos;
        _alumnoRepo = alumnoRepo;
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

        // Unirse = entrar a la LISTA DE ESPERA de ese club (sin aprobar). La ficha
        // nace EnEspera (Construir) en el tenant elegido; se vuelve alumno cuando el
        // profe le asigna una clase. El mensaje opcional queda como nota.
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
    // El portal se entera por su sesión (la ficha aparece EnEspera).
    public Task<IReadOnlyList<MiSolicitudDto>> MisAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MiSolicitudDto>>([]);

    /// <summary>Lista de espera del club: las fichas EnEspera (miembros sin clase todavía).</summary>
    public async Task<IReadOnlyList<SolicitudPendienteDto>> PendientesAsync(
        CancellationToken ct = default)
    {
        IEnumerable<Alumno> fichas = await _alumnoRepo.ListarAsync(null, EstadoAlumno.EnEspera, ct);

        // El profe EMPLEADO ve solo SU lista de espera (los que él cargó); el dueño, toda.
        if (_usuario.EsStaff)
            fichas = fichas.Where(a => a.ProfesorUserId == _usuario.UserId);

        return fichas
            .Select(a => new SolicitudPendienteDto
            {
                Id = a.Id, // ahora es el id de la FICHA (para quitarla de la lista)
                Nombre = a.Nombre,
                Apellido = a.Apellido,
                Email = a.Email ?? string.Empty,
                Dni = a.Dni,
                Telefono = a.Telefono,
                FechaNacimiento = a.FechaNacimiento,
                EsMenor = a.EsMenor,
                Categoria = a.Categoria.ToString(),
                Mensaje = a.Notas,
                CreadoEl = a.CreadoEl,
            })
            .ToList();
    }

    public async Task<int> ContarPendientesAsync(CancellationToken ct = default) =>
        // El staff cuenta solo los suyos (reusa el filtro de arriba); el dueño, un COUNT directo.
        _usuario.EsStaff
            ? (await PendientesAsync(ct)).Count
            : await _alumnoRepo.ContarPorEstadoAsync(EstadoAlumno.EnEspera, ct);

    /// <summary>Quitar de la lista de espera: borra la ficha EnEspera (conserva su login).</summary>
    public async Task QuitarDeEsperaAsync(Guid alumnoId, CancellationToken ct = default)
    {
        var ficha = await _alumnoRepo.ObtenerAsync(alumnoId, ct);
        if (ficha is null || ficha.Estado != EstadoAlumno.EnEspera)
            throw new ReglaDeNegocioException("Ese registro no está en la lista de espera.");

        // El staff solo quita de SU lista de espera.
        if (_usuario.EsStaff && ficha.ProfesorUserId != _usuario.UserId)
            throw new ReglaDeNegocioException("Ese registro no es de tu lista de espera.");

        // Borra la ficha y su historial (que no tiene, es un miembro sin clase). El
        // Usuario NO se toca: la persona puede unirse a otro club o volver a intentar.
        await _alumnoRepo.EliminarDefinitivoAsync(ficha, ct);
    }
}
