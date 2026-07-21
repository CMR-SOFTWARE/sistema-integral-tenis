namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Plantilla RECURRENTE semanal: "Intermedios, martes 18:00, 60', Cancha 1".
/// Dura toda la temporada; el cambio de temporada = editar/desactivar.
/// Apunta a un GRUPO (clase grupal) o a un ALUMNO (individual) — exactamente
/// uno de los dos (regla validada en el service).
/// </summary>
public class Horario
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid CanchaId { get; set; }
    public Cancha Cancha { get; set; } = null!; // la sede se deriva de la cancha

    /// <summary>El profe que da esta clase (dueño o staff); null = sin asignar. Sin nav, como UserId.</summary>
    public Guid? ProfesorUserId { get; set; }

    // ── Grupal XOR individual ──
    public Guid? GrupoId { get; set; }
    public Grupo? Grupo { get; set; }
    public Guid? AlumnoId { get; set; }
    public Alumno? Alumno { get; set; }

    // ── Recurrencia semanal ──
    public DayOfWeek Dia { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public int DuracionMinutos { get; set; } = 60;

    public bool Activo { get; set; } = true;
    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;

    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
}
