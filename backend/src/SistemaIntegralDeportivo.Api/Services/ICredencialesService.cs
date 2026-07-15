namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>El usuario recién creado y su temporal (se muestra UNA vez, no se persiste).</summary>
public record CredencialesCreadas(Guid UserId, string PasswordTemporal);

/// <summary>
/// Wrapper chico de Identity para el ALTA POR EL PROFE (plan v2: el registro
/// es una sola vez — el profe crea usuario + ficha juntos). Existe como
/// servicio propio porque UserManager es inmockeable en los tests de
/// AlumnoService; esto se mockea en una línea.
/// </summary>
public interface ICredencialesService
{
    /// <summary>
    /// Crea el Usuario global con contraseña TEMPORAL y DebeCambiarPassword=true.
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si el email ya tiene cuenta.</exception>
    Task<CredencialesCreadas> CrearConTemporalAsync(
        string email, string nombre, string apellido,
        string? dni, string? telefono, CancellationToken ct = default);

    /// <summary>Compensación: borra el usuario si la ficha no se pudo crear.</summary>
    Task EliminarAsync(Guid userId, CancellationToken ct = default);
}
