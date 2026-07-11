using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Acceso al tenant actual (demo hasta que haya auth) y su configuración.</summary>
public interface ITenantRepository
{
    Task<Tenant> ObtenerActualAsync(CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);

    /// <summary>¿El usuario es dueño de algún tenant? (membresía Profesor, ADR-0007).</summary>
    Task<bool> EsDuenioAsync(Guid userId, CancellationToken ct = default);
}
