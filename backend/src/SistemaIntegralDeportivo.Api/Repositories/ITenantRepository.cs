using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Acceso al tenant del request (ADR-0010) y su configuración.</summary>
public interface ITenantRepository
{
    Task<Tenant> ObtenerActualAsync(CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);

    /// <summary>El tenant que el usuario ADMINISTRA (membresía Profesor, ADR-0007); null si no es dueño.</summary>
    Task<Tenant?> ObtenerPorOwnerAsync(Guid userId, CancellationToken ct = default);
}
