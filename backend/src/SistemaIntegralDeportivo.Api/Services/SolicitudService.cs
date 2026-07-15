using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class SolicitudService : ISolicitudService
{
    private readonly ISolicitudRepository _solicitudes;
    private readonly IAlumnoService _alumnos;
    private readonly IAlumnoRepository _alumnoRepo;
    private readonly ITenantRepository _tenants;

    public SolicitudService(
        ISolicitudRepository solicitudes, IAlumnoService alumnos,
        IAlumnoRepository alumnoRepo, ITenantRepository tenants)
    {
        _solicitudes = solicitudes;
        _alumnos = alumnos;
        _alumnoRepo = alumnoRepo;
        _tenants = tenants;
    }

    public async Task<IReadOnlyList<MiSolicitudDto>> CrearAsync(
        Usuario usuario, CrearSolicitudDto dto, CancellationToken ct = default)
    {
        var club = await _tenants.ObtenerPorIdAsync(dto.TenantId, ct);
        if (club is null || club.Estado != EstadoTenant.Activo)
            throw new ReglaDeNegocioException("Ese club no existe o no está disponible.");

        // Un club por alumno POR AHORA (multi-club llega con la reserva de turnos)
        if (await _alumnoRepo.ObtenerPorUserIdAsync(usuario.Id, ct) is not null)
            throw new ReglaDeNegocioException(
                "Ya estás vinculado a un club. Por ahora se puede pertenecer a uno solo.");

        if (await _solicitudes.ExistePendienteAsync(usuario.Id, dto.TenantId, ct))
            throw new ReglaDeNegocioException("Ya tenés una solicitud pendiente en ese club.");

        // La ficha se arma con TUS datos: tienen que estar completos
        if (string.IsNullOrWhiteSpace(usuario.Dni) ||
            string.IsNullOrWhiteSpace(usuario.PhoneNumber) ||
            usuario.FechaNacimiento is null)
            throw new ReglaDeNegocioException(
                "Completá tu DNI, teléfono y fecha de nacimiento antes de solicitar (Mi perfil).");

        await _solicitudes.AgregarAsync(new Solicitud
        {
            UserId = usuario.Id,
            TenantId = dto.TenantId,
            Mensaje = string.IsNullOrWhiteSpace(dto.Mensaje) ? null : dto.Mensaje.Trim(),
        }, ct);
        await _solicitudes.GuardarCambiosAsync(ct);

        return await MisAsync(usuario.Id, ct);
    }

    public async Task<IReadOnlyList<MiSolicitudDto>> MisAsync(
        Guid userId, CancellationToken ct = default) =>
        (await _solicitudes.ListarPorUsuarioAsync(userId, ct))
            .Select(s => new MiSolicitudDto
            {
                Id = s.Id,
                Club = s.Tenant?.Nombre ?? string.Empty,
                Estado = s.Estado.ToString(),
                Mensaje = s.Mensaje,
                CreadoEl = s.CreadoEl,
                ResueltoEl = s.ResueltoEl,
            })
            .ToList();

    public async Task<IReadOnlyList<SolicitudPendienteDto>> PendientesAsync(
        CancellationToken ct = default) =>
        (await _solicitudes.ListarPendientesConUsuarioAsync(ct))
            .Select(x => new SolicitudPendienteDto
            {
                Id = x.Solicitud.Id,
                Nombre = x.Solicitante.Nombre,
                Apellido = x.Solicitante.Apellido,
                Email = x.Solicitante.Email ?? string.Empty,
                Dni = x.Solicitante.Dni,
                Telefono = x.Solicitante.PhoneNumber,
                FechaNacimiento = x.Solicitante.FechaNacimiento,
                EsMenor = x.Solicitante.FechaNacimiento is { } fn && CalcularEdad(fn) < 18,
                Categoria = x.Solicitante.Categoria?.ToString(),
                Mensaje = x.Solicitud.Mensaje,
                CreadoEl = x.Solicitud.CreadoEl,
            })
            .ToList();

    public Task<int> ContarPendientesAsync(CancellationToken ct = default) =>
        _solicitudes.ContarPendientesAsync(ct);

    public async Task<AlumnoResponseDto> AprobarAsync(
        Guid solicitudId, CancellationToken ct = default)
    {
        var fila = await _solicitudes.ObtenerPendienteConUsuarioAsync(solicitudId, ct)
            ?? throw new ReglaDeNegocioException("La solicitud no existe o ya fue resuelta.");
        var (solicitud, solicitante) = fila;

        // Los datos del registro viajan a la ficha. Si algo falla (p.ej.
        // menor sin tutor), la excepción sube y la solicitud QUEDA pendiente.
        var ficha = await _alumnos.CrearVinculadoAsync(new CreateAlumnoDto
        {
            Nombre = solicitante.Nombre,
            Apellido = solicitante.Apellido,
            Dni = solicitante.Dni ?? string.Empty,
            Telefono = solicitante.PhoneNumber ?? string.Empty,
            Email = solicitante.Email ?? string.Empty,
            FechaNacimiento = solicitante.FechaNacimiento
                ?? throw new ReglaDeNegocioException(
                    "El solicitante no cargó su fecha de nacimiento."),
            Categoria = solicitante.Categoria ?? CategoriaAlumno.SinCategoria,
            ConsentimientoDatos = true, // lo dio él mismo al registrarse
        }, solicitud.UserId, ct);

        solicitud.Estado = EstadoSolicitud.Aprobada;
        solicitud.AlumnoId = ficha.Id;
        solicitud.ResueltoEl = DateTime.UtcNow;
        await _solicitudes.GuardarCambiosAsync(ct);

        return ficha;
    }

    public async Task RechazarAsync(Guid solicitudId, CancellationToken ct = default)
    {
        var fila = await _solicitudes.ObtenerPendienteConUsuarioAsync(solicitudId, ct)
            ?? throw new ReglaDeNegocioException("La solicitud no existe o ya fue resuelta.");

        fila.Solicitud.Estado = EstadoSolicitud.Rechazada;
        fila.Solicitud.ResueltoEl = DateTime.UtcNow;
        await _solicitudes.GuardarCambiosAsync(ct);
    }

    private static int CalcularEdad(DateTime nacimiento)
    {
        var hoy = DateTime.UtcNow.Date;
        var edad = hoy.Year - nacimiento.Year;
        if (nacimiento.Date > hoy.AddYears(-edad)) edad--;
        return edad;
    }
}
