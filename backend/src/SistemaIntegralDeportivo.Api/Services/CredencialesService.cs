using Microsoft.AspNetCore.Identity;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Services;

public class CredencialesService : ICredencialesService
{
    private readonly UserManager<Usuario> _userManager;

    public CredencialesService(UserManager<Usuario> userManager)
    {
        _userManager = userManager;
    }

    public async Task<CredencialesCreadas> CrearConTemporalAsync(
        string email, string nombre, string apellido,
        string? dni, string? telefono, CancellationToken ct = default)
    {
        var normalizado = email.Trim();
        if (await _userManager.FindByEmailAsync(normalizado) is not null)
            throw new ReglaDeNegocioException(
                $"El email {normalizado} ya tiene una cuenta en la plataforma. " +
                "Pedile al alumno que te envíe una solicitud desde su portal (Mi club) y aprobala desde Solicitudes.");

        // Decisión de producto: la contraseña inicial es el TELÉFONO del
        // alumno (fácil de comunicar); cambiarla es opcional, desde su perfil
        var passwordInicial = new string((telefono ?? string.Empty)
            .Where(char.IsAsciiLetterOrDigit).ToArray());
        if (passwordInicial.Length < 8)
            throw new ReglaDeNegocioException(
                "El teléfono es la contraseña inicial del alumno: necesita al menos 8 dígitos.");

        var usuario = new Usuario
        {
            UserName = normalizado,
            Email = normalizado,
            Nombre = nombre.Trim(),
            Apellido = apellido.Trim(),
            Dni = string.IsNullOrWhiteSpace(dni) ? null : dni.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim(),
            DebeCambiarPassword = false, // el cambio es opcional (decisión de Lucas)
        };

        var resultado = await _userManager.CreateAsync(usuario, passwordInicial);
        if (!resultado.Succeeded)
            throw new ReglaDeNegocioException(
                string.Join(" ", resultado.Errors.Select(e => e.Description).Distinct()));

        return new CredencialesCreadas(usuario.Id, passwordInicial);
    }

    public async Task EliminarAsync(Guid userId, CancellationToken ct = default)
    {
        var usuario = await _userManager.FindByIdAsync(userId.ToString());
        if (usuario is not null)
            await _userManager.DeleteAsync(usuario);
    }
}
