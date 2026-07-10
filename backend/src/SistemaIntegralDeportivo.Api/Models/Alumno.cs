namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Entidad de datos pura: NO tiene password ni login (eso es fase futura).
/// Cuando el alumno se registre, se vincula vía UserId (nullable hoy),
/// sin migración de schema.
/// </summary>
public class Alumno
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── Tenant (dueño) ──
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    // ── Datos personales ──
    public required string Nombre { get; set; }
    public required string Apellido { get; set; }
    public required string Dni { get; set; }
    public string? Email { get; set; }            // opcional: hay gente sin email
    public required string Telefono { get; set; } // formato E.164 (+549...) para WhatsApp
    public DateTime FechaNacimiento { get; set; } // NO guardamos "esMenor": se calcula
    public string? FotoUrl { get; set; }

    // ── Datos deportivos ──
    public CategoriaAlumno Categoria { get; set; } = CategoriaAlumno.SinCategoria;

    // ── Arancel (valor de cuota mensual; viene del modal del diseño) ──
    public decimal? Arancel { get; set; }

    // ── Ciclo de vida ──
    public EstadoAlumno Estado { get; set; } = EstadoAlumno.Activo;
    public string? Notas { get; set; } // observaciones del profe ("lesión de hombro", etc.)

    // ── Cómo liquida (ADR-0009): el mes entero (vence el 10) o cargo por cargo ──
    public ModalidadPago Modalidad { get; set; } = ModalidadPago.Mensual;

    // ── Consentimientos (Ley 25.326): no alcanza un bool, hay que poder
    //    demostrar CUÁNDO se consintió ──
    public bool ConsentimientoWhatsapp { get; set; }
    public DateTime? ConsentimientoWhatsappEl { get; set; }
    public bool ConsentimientoDatos { get; set; } // si es menor, lo da el tutor
    public DateTime? ConsentimientoDatosEl { get; set; }

    // ── Tutor (solo menores) ──
    public Guid? TutorId { get; set; }
    public Tutor? Tutor { get; set; }

    // ── Sede a la que pertenece (informativo, para filtrar; opcional) ──
    public Guid? SedeId { get; set; }
    public Sede? Sede { get; set; }

    // ── Futuro login (no se usa todavía) ──
    public Guid? UserId { get; set; }

    // ── Auditoría ──
    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
    public DateTime ActualizadoEl { get; set; } = DateTime.UtcNow;

    // ── Relaciones ──
    public ICollection<AlumnoGrupo> Grupos { get; set; } = new List<AlumnoGrupo>();
}
