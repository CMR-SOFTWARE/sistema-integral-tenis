using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Data;

/// <summary>
/// Seed de auth: crea el usuario PROFE dueño del tenant demo si no existe.
/// No puede ir en HasData porque el hash de contraseña no es determinístico.
/// </summary>
public static class AuthSeeder
{
    // Credenciales del prototipo (cambiar al desplegar; ver plan v2: el alta
    // real de profesores nace del registro + checkout Mercado Pago)
    public const string EmailProfe = "profe@clubdemo.com";
    public const string PasswordProfe = "profe1234";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<Usuario>>();
        var db = services.GetRequiredService<AppDbContext>();

        var profe = await userManager.FindByEmailAsync(EmailProfe);
        if (profe is null)
        {
            profe = new Usuario
            {
                UserName = EmailProfe,
                Email = EmailProfe,
                Nombre = "Profe",
                Apellido = "Demo",
            };
            var resultado = await userManager.CreateAsync(profe, PasswordProfe);
            if (!resultado.Succeeded)
                throw new InvalidOperationException(
                    "No se pudo sembrar el profe demo: " +
                    string.Join("; ", resultado.Errors.Select(e => e.Description)));
        }

        // La membresía mínima del ADR-0007: el profe es dueño del tenant demo
        var tenant = await db.Tenants.FirstAsync(t => t.Id == AppDbContext.TenantDemoId);
        if (tenant.OwnerUserId != profe.Id)
        {
            tenant.OwnerUserId = profe.Id;
            await db.SaveChangesAsync();
        }
    }
}
