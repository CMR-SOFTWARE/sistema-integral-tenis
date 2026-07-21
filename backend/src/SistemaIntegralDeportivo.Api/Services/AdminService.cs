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
}

public class AdminService : IAdminService
{
    private readonly IAdminRepository _repo;

    public AdminService(IAdminRepository repo)
    {
        _repo = repo;
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
            TotalAlumnos = await _repo.ContarAlumnosActivosAsync(ct),
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
}
