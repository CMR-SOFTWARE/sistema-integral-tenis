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
        string telefono, string nombre, string apellido,
        string? dni, string? email, CancellationToken ct = default)
    {
        // El TELÉFONO es el usuario (UserName) y la contraseña inicial. Solo dígitos,
        // así el alumno entra escribiendo su celular sin acordarse de formatos.
        var usuario = SoloDigitos(telefono);
        if (usuario.Length < 8)
            throw new ReglaDeNegocioException(
                "El teléfono es el usuario y la contraseña inicial: necesita al menos 8 dígitos.");

        if (await _userManager.FindByNameAsync(usuario) is not null)
            throw new ReglaDeNegocioException(
                $"El celular {telefono} ya tiene una cuenta en la plataforma. Si es la misma " +
                "persona, pedile que te mande una solicitud desde su portal (Mi club) y aprobala desde Solicitudes.");

        var nuevo = new Usuario
        {
            UserName = usuario,
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            Nombre = nombre.Trim(),
            Apellido = apellido.Trim(),
            Dni = string.IsNullOrWhiteSpace(dni) ? null : dni.Trim(),
            PhoneNumber = telefono.Trim(),
            DebeCambiarPassword = false, // el cambio es opcional (decisión de Lucas)
        };

        // La contraseña inicial es el mismo teléfono (dígitos)
        var resultado = await _userManager.CreateAsync(nuevo, usuario);
        if (!resultado.Succeeded)
            throw new ReglaDeNegocioException(
                string.Join(" ", resultado.Errors.Select(e => e.Description).Distinct()));

        return new CredencialesCreadas(nuevo.Id, usuario);
    }

    public async Task<bool> TelefonoTieneCuentaAsync(string telefono, CancellationToken ct = default) =>
        await _userManager.FindByNameAsync(SoloDigitos(telefono)) is not null;

    public async Task EliminarAsync(Guid userId, CancellationToken ct = default)
    {
        var usuario = await _userManager.FindByIdAsync(userId.ToString());
        if (usuario is not null)
            await _userManager.DeleteAsync(usuario);
    }

    /// <summary>Deja solo los dígitos (el UserName y la clave son el celular sin formato).</summary>
    private static string SoloDigitos(string? s) =>
        new((s ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
}
