using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Acceso al tenant del request (ADR-0010) y su configuración.</summary>
public interface ITenantRepository
{
    Task<Tenant> ObtenerActualAsync(CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);

    /// <summary>El tenant que el usuario ADMINISTRA (membresía Profesor, ADR-0007); null si no es dueño.</summary>
    Task<Tenant?> ObtenerPorOwnerAsync(Guid userId, CancellationToken ct = default);

    /// <summary>¿El subdominio ya está tomado? (para el slug del registro de profe).</summary>
    Task<bool> ExisteSubdominioAsync(string subdominio, CancellationToken ct = default);

    Task AgregarAsync(Tenant tenant, CancellationToken ct = default);

    /// <summary>
    /// Profesores ACTIVOS con el nombre de su dueño, para la búsqueda pública
    /// (registro/solicitudes). Filtro opcional por nombre de club o del profe.
    /// </summary>
    Task<IReadOnlyList<(Tenant Tenant, string Profesor)>> ListarActivosAsync(
        string? buscar, CancellationToken ct = default);
}
