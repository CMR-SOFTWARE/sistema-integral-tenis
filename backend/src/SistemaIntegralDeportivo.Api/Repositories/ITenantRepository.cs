using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Acceso al tenant actual (demo hasta que haya auth) y su configuración.</summary>
public interface ITenantRepository
{
    Task<Tenant> ObtenerActualAsync(CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}
