using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Resolución del tenant del request (ADR-0010): override > claim > excepción.
/// Fail-fast: sin tenant NUNCA se cae a un default silencioso.
/// </summary>
public class TenantActualTests
{
    private static TenantActual ConClaims(params Claim[] claims)
    {
        var http = new Mock<IHttpContextAccessor>();
        http.Setup(h => h.HttpContext).Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims)),
        });
        return new TenantActual(http.Object);
    }

    [Fact]
    public void ConClaimTenant_LoDevuelve()
    {
        var tenantId = Guid.NewGuid();
        var actual = ConClaims(new Claim("tenant", tenantId.ToString()));

        Assert.Equal(tenantId, actual.TenantId);
    }

    [Fact]
    public void SinClaim_Lanza()
    {
        var actual = ConClaims(); // usuario sin claim tenant (alumno, o sin login)

        Assert.Throws<InvalidOperationException>(() => actual.TenantId);
    }

    [Fact]
    public void ClaimMalformado_Lanza()
    {
        var actual = ConClaims(new Claim("tenant", "esto-no-es-un-guid"));

        Assert.Throws<InvalidOperationException>(() => actual.TenantId);
    }

    [Fact]
    public void Establecer_PisaAlClaim()
    {
        var delToken = Guid.NewGuid();
        var delPortal = Guid.NewGuid();
        var actual = ConClaims(new Claim("tenant", delToken.ToString()));

        actual.Establecer(delPortal);

        Assert.Equal(delPortal, actual.TenantId);
    }

    [Fact]
    public void SinHttpContext_Lanza()
    {
        var http = new Mock<IHttpContextAccessor>();
        http.Setup(h => h.HttpContext).Returns((HttpContext?)null);
        var actual = new TenantActual(http.Object);

        Assert.Throws<InvalidOperationException>(() => actual.TenantId);
    }
}
