using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Quién hace el request y con qué rol, sacado del token (claims sub + rol).
/// Sirve para filtrar datos por profe: el staff ve solo lo suyo, el dueño todo.
/// </summary>
public interface IUsuarioActual
{
    /// <summary>El userId del token (claim sub); null si no hay usuario.</summary>
    Guid? UserId { get; }
    /// <summary>Es profe EMPLEADO (claim rol=staff): ve solo lo asignado a él.</summary>
    bool EsStaff { get; }
    /// <summary>Es DUEÑO del club (claim rol=owner): ve todo.</summary>
    bool EsDueño { get; }
}

/// <summary>Scoped: vive lo que dura el request.</summary>
public class UsuarioActual : IUsuarioActual
{
    private readonly IHttpContextAccessor _http;

    public UsuarioActual(IHttpContextAccessor http)
    {
        _http = http;
    }

    private ClaimsPrincipal? User => _http.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(User?.FindFirst("sub")?.Value, out var id) ? id : null;

    public bool EsStaff => User?.FindFirst("rol")?.Value == "staff";

    public bool EsDueño => User?.FindFirst("rol")?.Value == "owner";
}
