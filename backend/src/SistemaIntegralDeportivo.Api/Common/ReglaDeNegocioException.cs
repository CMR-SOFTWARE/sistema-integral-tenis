namespace SistemaIntegralDeportivo.Api.Common;

/// <summary>
/// Violación de una regla de negocio (ej: menor sin tutor, DNI duplicado).
/// El controller la traduce a 400/409; nunca es un error del servidor.
/// </summary>
public class ReglaDeNegocioException : Exception
{
    public ReglaDeNegocioException(string message) : base(message) { }
}
