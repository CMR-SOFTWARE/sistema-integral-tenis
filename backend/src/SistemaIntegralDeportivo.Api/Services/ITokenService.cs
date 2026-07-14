using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Emite el JWT que el front manda en Authorization: Bearer.</summary>
public interface ITokenService
{
    /// <summary>
    /// Claims: sub (userId), email, nombre; si <paramref name="tenantPropio"/>
    /// viene, agrega "profesor" y "tenant" (el club que administra, ADR-0010).
    /// La ficha de alumno NO va en el token (puede vincularse después de
    /// emitido): el portal la resuelve por userId en cada request.
    /// </summary>
    string Generar(Usuario usuario, Tenant? tenantPropio);
}
