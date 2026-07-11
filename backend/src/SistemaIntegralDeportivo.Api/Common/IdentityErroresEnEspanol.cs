using Microsoft.AspNetCore.Identity;

namespace SistemaIntegralDeportivo.Api.Common;

/// <summary>
/// Los errores de Identity que puede ver el usuario final, en español.
/// Como UserName == Email en este sistema, DuplicateUserName y
/// DuplicateEmail dicen lo mismo (el controller deduplica).
/// </summary>
public class IdentityErroresEnEspanol : IdentityErrorDescriber
{
    public override IdentityError DuplicateUserName(string userName) => new()
    {
        Code = nameof(DuplicateUserName),
        Description = $"Ya existe una cuenta con el email {userName}. Iniciá sesión.",
    };

    public override IdentityError DuplicateEmail(string email) => new()
    {
        Code = nameof(DuplicateEmail),
        Description = $"Ya existe una cuenta con el email {email}. Iniciá sesión.",
    };

    public override IdentityError InvalidEmail(string? email) => new()
    {
        Code = nameof(InvalidEmail),
        Description = $"El email '{email}' no es válido.",
    };

    public override IdentityError InvalidUserName(string? userName) => new()
    {
        Code = nameof(InvalidUserName),
        Description = $"El email '{userName}' no es válido.",
    };

    public override IdentityError PasswordTooShort(int length) => new()
    {
        Code = nameof(PasswordTooShort),
        Description = $"La contraseña tiene que tener al menos {length} caracteres.",
    };
}
