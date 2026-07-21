using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string Generar(Usuario usuario, Tenant? tenant, RolTenant? rol)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email ?? string.Empty),
            new("nombre", $"{usuario.Nombre} {usuario.Apellido}"),
        };
        if (tenant is not null)
        {
            claims.Add(new Claim("profesor", "true"));
            // El club en el que trabaja: los repos de gestión operan este tenant (ADR-0010)
            claims.Add(new Claim("tenant", tenant.Id.ToString()));
            // Dueño (head pro) vs Staff (profe empleado): el front y las policies lo usan
            claims.Add(new Claim("rol", rol == RolTenant.Dueño ? "owner" : "staff"));
        }

        // Admin de plataforma (cross-tenant): habilita el panel "Plataforma"
        if (usuario.EsAdminPlataforma)
            claims.Add(new Claim("admin", "true"));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]
            ?? throw new InvalidOperationException("Falta Jwt:Key en la configuración.")));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7), // prototipo: sin refresh token todavía
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
