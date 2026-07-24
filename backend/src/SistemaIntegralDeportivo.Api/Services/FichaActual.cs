using Microsoft.AspNetCore.Http;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// La ficha ACTIVA del request en el portal (Capa 2, cuenta familiar): el
/// titular elige con un selector qué miembro está viendo, y el front manda ese
/// id en el header <c>X-Alumno-Id</c>. Null = usar la ficha por defecto.
/// El service valida que la ficha pertenezca al titular logueado.
/// </summary>
public interface IFichaActual
{
    Guid? AlumnoId { get; }
}

/// <summary>Scoped: vive lo que dura el request.</summary>
public class FichaActual : IFichaActual
{
    private readonly IHttpContextAccessor _http;

    public FichaActual(IHttpContextAccessor http)
    {
        _http = http;
    }

    public Guid? AlumnoId =>
        Guid.TryParse(_http.HttpContext?.Request.Headers["X-Alumno-Id"].FirstOrDefault(), out var id)
            ? id
            : null;
}
