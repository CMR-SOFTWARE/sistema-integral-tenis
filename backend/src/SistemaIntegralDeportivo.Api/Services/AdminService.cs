using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Panel de plataforma (cross-tenant): métricas globales y gestión de clubes.</summary>
public interface IAdminService
{
    Task<MetricasPlataformaDto> MetricasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ClubAdminDto>> ListarClubesAsync(CancellationToken ct = default);
    /// <summary>Activa o suspende un club (solo esos dos estados).</summary>
    Task CambiarEstadoClubAsync(Guid tenantId, EstadoTenant estado, CancellationToken ct = default);

    /// <summary>
    /// Alta de una academia desde Plataforma (Bloque 6, pedido 10): crea el club y la
    /// cuenta del director, y la salta directo a Activa (sin checkout de Mercado Pago).
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Nombre vacío o teléfono ya en uso.</exception>
    Task<ClubCreadoDto> CrearClubAsync(AltaClubDto dto, CancellationToken ct = default);

    /// <summary>El padrón de personas de la plataforma (Bloque 6, pedido 11).</summary>
    Task<IReadOnlyList<PersonaAdminDto>> ListarPersonasAsync(CancellationToken ct = default);
}

public class AdminService : IAdminService
{
    private readonly IAdminRepository _repo;
    private readonly ICredencialesService _credenciales;
    private readonly IMembresiaTenantRepository _membresias;
    private readonly IAuthService _auth;

    public AdminService(
        IAdminRepository repo, ICredencialesService credenciales,
        IMembresiaTenantRepository membresias, IAuthService auth)
    {
        _repo = repo;
        _credenciales = credenciales;
        _membresias = membresias;
        _auth = auth;
    }

    public async Task<MetricasPlataformaDto> MetricasAsync(CancellationToken ct = default)
    {
        var hoy = DateTime.UtcNow;
        var hace30 = hoy.AddDays(-30);

        var porEstado = await _repo.ContarClubesPorEstadoAsync(ct);
        var staff = await _repo.ContarStaffActivosAsync(ct);

        int Estado(EstadoTenant e) => porEstado.TryGetValue(e, out var n) ? n : 0;
        var totalClubes = porEstado.Values.Sum();

        return new MetricasPlataformaDto
        {
            TotalClubes = totalClubes,
            ClubesActivos = Estado(EstadoTenant.Activo),
            ClubesPendientes = Estado(EstadoTenant.PendientePago),
            ClubesSuspendidos = Estado(EstadoTenant.Suspendido),
            TotalProfes = totalClubes + staff, // dueños + staff
            TotalUsuarios = await _repo.ContarUsuariosAsync(ct),
            IngresosMes = await _repo.IngresosDelMesAsync(hoy.Year, hoy.Month, ct),
            ClubesNuevos30d = await _repo.ContarClubesNuevosAsync(hace30, ct),
            AlumnosNuevos30d = await _repo.ContarAlumnosNuevosAsync(hace30, ct),
        };
    }

    public Task<IReadOnlyList<ClubAdminDto>> ListarClubesAsync(CancellationToken ct = default) =>
        _repo.ListarClubesAsync(ct);

    public async Task CambiarEstadoClubAsync(Guid tenantId, EstadoTenant estado, CancellationToken ct = default)
    {
        if (estado is not (EstadoTenant.Activo or EstadoTenant.Suspendido))
            throw new ReglaDeNegocioException("Un club solo se puede activar o suspender.");

        var club = await _repo.ObtenerTenantAsync(tenantId, ct)
            ?? throw new ReglaDeNegocioException("El club no existe.");

        club.Estado = estado;
        await _repo.GuardarCambiosAsync(ct);
    }

    public async Task<ClubCreadoDto> CrearClubAsync(AltaClubDto dto, CancellationToken ct = default)
    {
        // Chequeo previo con mensaje propio: el genérico de CrearConTemporalAsync habla
        // de mandar una solicitud desde el portal, que no aplica en este flujo (acá es
        // el admin dando de alta una academia nueva, no un alumno pidiendo lugar).
        if (await _credenciales.BuscarTitularPorTelefonoAsync(dto.Telefono, ct) is not null)
            throw new ReglaDeNegocioException(
                $"El celular {dto.Telefono} ya tiene una cuenta en la plataforma. " +
                "Dale de alta con otro número, así el director tiene su cuenta propia.");

        var cred = await _credenciales.CrearConTemporalAsync(
            dto.Telefono, dto.Nombre, dto.Apellido, dni: null, email: dto.Email, ct);

        var usuario = await _membresias.ObtenerUsuarioAsync(cred.UserId, ct)
            ?? throw new ReglaDeNegocioException("No se pudo crear la cuenta del director.");

        var tenant = await _auth.CrearTenantParaAsync(usuario, dto.NombreClub, ct);
        // Salta el checkout de Mercado Pago: nace directo Activa (Bloque 6, pedido 10).
        // Misma costura que usa el webhook real de MP (ADR: ActivarTenantAsync es idempotente).
        await _auth.ActivarTenantAsync(usuario, ct);

        return new ClubCreadoDto
        {
            Club = new ClubAdminDto
            {
                Id = tenant.Id,
                Nombre = tenant.Nombre,
                Subdominio = tenant.Subdominio,
                Estado = EstadoTenant.Activo.ToString(),
                Profesor = $"{usuario.Nombre} {usuario.Apellido}",
                Alumnos = 0,
                CreadoEl = tenant.CreadoEl,
            },
            Usuario = cred.PasswordTemporal,
            PasswordTemporal = cred.PasswordTemporal,
        };
    }

    public Task<IReadOnlyList<PersonaAdminDto>> ListarPersonasAsync(CancellationToken ct = default) =>
        _repo.ListarPersonasAsync(ct);
}
