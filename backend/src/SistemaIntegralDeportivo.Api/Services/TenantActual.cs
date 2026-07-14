using Microsoft.AspNetCore.Http;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Scoped: vive lo que dura el request (como el DbContext).</summary>
public class TenantActual : ITenantActual
{
    private readonly IHttpContextAccessor _http;
    private Guid? _override;

    public TenantActual(IHttpContextAccessor http)
    {
        _http = http;
    }

    public void Establecer(Guid tenantId) => _override = tenantId;

    public Guid TenantId
    {
        get
        {
            if (_override is { } fijado) return fijado;

            var claim = _http.HttpContext?.User.FindFirst("tenant")?.Value;
            if (claim is not null && Guid.TryParse(claim, out var delToken)) return delToken;

            throw new InvalidOperationException(
                "No hay tenant en el contexto: falta el claim 'tenant' del token " +
                "o el override del portal (ADR-0010). Nunca se cae a un default.");
        }
    }
}
