using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Emite el JWT que el front manda en Authorization: Bearer.</summary>
public interface ITokenService
{
    /// <summary>
    /// Claims: sub (userId), email, nombre y "profesor" si es dueño de un
    /// tenant. La ficha de alumno NO va en el token (puede reclamarse después
    /// de emitido): el portal la resuelve por userId en cada request.
    /// </summary>
    string Generar(Usuario usuario, bool esProfesor);
}
