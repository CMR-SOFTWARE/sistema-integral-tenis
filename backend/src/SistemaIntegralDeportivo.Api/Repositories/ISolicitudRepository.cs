using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>
/// Datos de solicitudes alumno→profe. MIXTO a propósito: lo del jugador es
/// GLOBAL (por userId, como ObtenerPorUserIdAsync); lo del profe scopea por
/// ITenantActual (su claim).
/// </summary>
public interface ISolicitudRepository
{
    // ── Lado jugador (global) ──

    /// <summary>Mis solicitudes con su Tenant, la más nueva primero.</summary>
    Task<IReadOnlyList<Solicitud>> ListarPorUsuarioAsync(Guid userId, CancellationToken ct = default);

    Task<bool> ExistePendienteAsync(Guid userId, Guid tenantId, CancellationToken ct = default);

    // ── Lado profe (scoped al tenant del claim) ──

    /// <summary>Pendientes de MI club con los datos del solicitante (join con Identity).</summary>
    Task<IReadOnlyList<(Solicitud Solicitud, Usuario Solicitante)>> ListarPendientesConUsuarioAsync(
        CancellationToken ct = default);

    /// <summary>Una pendiente de MI club (null si no existe, no es mía o ya se resolvió).</summary>
    Task<(Solicitud Solicitud, Usuario Solicitante)?> ObtenerPendienteConUsuarioAsync(
        Guid id, CancellationToken ct = default);

    Task<int> ContarPendientesAsync(CancellationToken ct = default);

    Task AgregarAsync(Solicitud solicitud, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
