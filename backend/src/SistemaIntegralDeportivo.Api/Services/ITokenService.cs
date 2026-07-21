using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Emite el JWT que el front manda en Authorization: Bearer.</summary>
public interface ITokenService
{
    /// <summary>
    /// Claims: sub (userId), email, nombre; si <paramref name="tenant"/> viene,
    /// agrega "profesor", "tenant" (el club en el que trabaja, ADR-0010) y "rol"
    /// (owner|staff, según <paramref name="rol"/>). La ficha de alumno NO va en el
    /// token (puede vincularse después): el portal la resuelve por userId.
    /// </summary>
    string Generar(Usuario usuario, Tenant? tenant, RolTenant? rol);
}
