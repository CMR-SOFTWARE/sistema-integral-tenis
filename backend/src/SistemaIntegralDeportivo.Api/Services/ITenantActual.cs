namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// El tenant sobre el que opera ESTE request (ADR-0010, reemplaza al tenant
/// demo fijo del ADR-0004). Precedencia: override explícito (flujos de
/// alumno: el portal lo fija desde su ficha) > claim "tenant" del JWT (el
/// profe) > excepción. Fail-fast a propósito: un request de gestión sin
/// tenant es un bug — NUNCA se cae silenciosamente a un tenant por defecto.
/// </summary>
public interface ITenantActual
{
    /// <exception cref="InvalidOperationException">Sin claim ni override.</exception>
    Guid TenantId { get; }

    /// <summary>Fija el tenant del request (portal alumno: desde su ficha).</summary>
    void Establecer(Guid tenantId);
}
