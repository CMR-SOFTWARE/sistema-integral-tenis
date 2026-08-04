namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// La carta de presentación de un profe dentro de un club: quién es, qué hizo y
/// fotos de su trabajo. La editan tanto el dueño como sus profes empleados, y la
/// ven los alumnos del club (y quien está buscando a quién sumarse).
///
/// Se identifica por (TenantId, UserId) y no por la membresía, porque el dueño no
/// tiene <see cref="MembresiaTenant"/> (se resuelve por <see cref="Tenant.OwnerUserId"/>)
/// y así la misma tabla sirve para los dos. Si la misma persona da clases en dos
/// clubes, tiene un perfil por club: son dos presentaciones distintas.
/// </summary>
public class PerfilProfesor
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>El usuario (identidad global) dueño de este perfil.</summary>
    public Guid UserId { get; set; }

    /// <summary>Cómo se presenta ("Profesor Nacional de Tenis"). Es el subtítulo bajo su nombre.</summary>
    public string? Titular { get; set; }

    /// <summary>Una línea corta de gancho ("Formando jugadores en Río Cuarto desde 2010").</summary>
    public string? Subtitulo { get; set; }

    /// <summary>El "quién soy" en texto libre.</summary>
    public string? Bio { get; set; }

    /// <summary>Los chips que se muestran arriba ("Alto rendimiento", "Niños"). Npgsql los mapea a text[].</summary>
    public List<string> Especialidades { get; set; } = [];

    /// <summary>Imagen ancha de fondo del encabezado.</summary>
    public string? PortadaUrl { get; set; }
    /// <summary>Ruta del archivo en el storage: es lo que necesita el borrado (la URL no sirve).</summary>
    public string? PortadaRuta { get; set; }

    public string? AvatarUrl { get; set; }
    public string? AvatarRuta { get; set; }

    /// <summary>
    /// Mientras esté en false el perfil no lo ve nadie: el profe lo arma tranquilo
    /// y lo publica cuando está conforme.
    /// </summary>
    public bool Publicado { get; set; }

    public ICollection<FotoPerfil> Fotos { get; set; } = [];
    public ICollection<HitoTrayectoria> Hitos { get; set; } = [];

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
    public DateTime ActualizadoEl { get; set; } = DateTime.UtcNow;
}
