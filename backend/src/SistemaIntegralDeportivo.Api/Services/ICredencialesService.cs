namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>El usuario recién creado y su temporal (se muestra UNA vez, no se persiste).</summary>
public record CredencialesCreadas(Guid UserId, string PasswordTemporal);

/// <summary>El titular (Usuario) dueño de un celular, para sumar fichas a su familia.</summary>
public record TitularInfo(Guid UserId, string Nombre, string Apellido);

/// <summary>
/// Wrapper chico de Identity para el ALTA POR EL PROFE (plan v2: el registro
/// es una sola vez — el profe crea usuario + ficha juntos). Existe como
/// servicio propio porque UserManager es inmockeable en los tests de
/// AlumnoService; esto se mockea en una línea.
/// </summary>
public interface ICredencialesService
{
    /// <summary>
    /// Crea el Usuario global usando el TELÉFONO como usuario (UserName) y como
    /// contraseña inicial. DNI y email son opcionales (no son la llave de login).
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si ese teléfono ya tiene cuenta.</exception>
    Task<CredencialesCreadas> CrearConTemporalAsync(
        string telefono, string nombre, string apellido,
        string? dni, string? email, CancellationToken ct = default);

    /// <summary>
    /// El titular dueño de ese celular, si existe (para sumar la ficha nueva a su
    /// familia en vez de crear otra cuenta). Null si el celular está libre.
    /// </summary>
    Task<TitularInfo?> BuscarTitularPorTelefonoAsync(string telefono, CancellationToken ct = default);

    /// <summary>Compensación: borra el usuario si la ficha no se pudo crear.</summary>
    Task EliminarAsync(Guid userId, CancellationToken ct = default);
}
