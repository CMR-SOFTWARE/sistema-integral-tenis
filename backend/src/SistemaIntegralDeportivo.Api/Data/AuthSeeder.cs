using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Data;

/// <summary>
/// Seed de auth: crea el usuario PROFE dueño del tenant demo, pero SOLO si el
/// tenant demo existe (lo siembra <c>HasData</c>, presente en desarrollo). En
/// producción el Club Demo se borra a mano; una vez borrado, este seeder no lo
/// recrea ni revive el admin de credenciales conocidas (profe@clubdemo.com),
/// que no debe existir en prod.
/// No puede ir en HasData porque el hash de contraseña no es determinístico.
/// </summary>
public static class AuthSeeder
{
    // Credenciales del prototipo (solo dev; el alta real de profes nace del
    // registro + checkout Mercado Pago). El usuario de login es el celular;
    // el email queda como dato opcional (y sirve para el fallback de login).
    public const string TelefonoProfe = "1122334455";
    public const string EmailProfe = "profe@clubdemo.com";
    public const string PasswordProfe = "profe1234";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();

        // Sin tenant demo no sembramos nada (caso producción). Antes esto usaba
        // FirstAsync y tiraba excepción si el demo no estaba → la API no arrancaba.
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == AppDbContext.TenantDemoId);
        if (tenant is null)
            return;

        var userManager = services.GetRequiredService<UserManager<Usuario>>();

        var profe = await userManager.FindByEmailAsync(EmailProfe);
        if (profe is null)
        {
            profe = new Usuario
            {
                UserName = TelefonoProfe, // el usuario de login es el celular
                Email = EmailProfe,
                PhoneNumber = TelefonoProfe,
                Nombre = "Profe",
                Apellido = "Demo",
                EsAdminPlataforma = true, // el profe demo es también el admin de la app
            };
            var resultado = await userManager.CreateAsync(profe, PasswordProfe);
            if (!resultado.Succeeded)
                throw new InvalidOperationException(
                    "No se pudo sembrar el profe demo: " +
                    string.Join("; ", resultado.Errors.Select(e => e.Description)));
        }
        else if (!profe.EsAdminPlataforma)
        {
            // Cuenta ya sembrada antes de este campo: la marcamos admin
            profe.EsAdminPlataforma = true;
            await userManager.UpdateAsync(profe);
        }

        // La membresía mínima del ADR-0007: el profe es dueño del tenant demo
        if (tenant.OwnerUserId != profe.Id)
        {
            tenant.OwnerUserId = profe.Id;
            await db.SaveChangesAsync();
        }
    }
}
