using Microsoft.AspNetCore.Identity;

namespace SistemaIntegralDeportivo.Api.Common;

/// <summary>
/// Los errores de Identity que puede ver el usuario final, en español.
/// El UserName es el celular; el email es opcional (ya no es la llave).
/// </summary>
public class IdentityErroresEnEspanol : IdentityErrorDescriber
{
    public override IdentityError DuplicateUserName(string userName) => new()
    {
        Code = nameof(DuplicateUserName),
        Description = $"Ya existe una cuenta con el celular {userName}. Iniciá sesión.",
    };

    public override IdentityError DuplicateEmail(string email) => new()
    {
        Code = nameof(DuplicateEmail),
        Description = $"Ya existe una cuenta con el email {email}.",
    };

    public override IdentityError InvalidEmail(string? email) => new()
    {
        Code = nameof(InvalidEmail),
        Description = $"El email '{email}' no es válido.",
    };

    public override IdentityError InvalidUserName(string? userName) => new()
    {
        Code = nameof(InvalidUserName),
        Description = $"El celular '{userName}' no es válido.",
    };

    public override IdentityError PasswordTooShort(int length) => new()
    {
        Code = nameof(PasswordTooShort),
        Description = $"La contraseña tiene que tener al menos {length} caracteres.",
    };
}
