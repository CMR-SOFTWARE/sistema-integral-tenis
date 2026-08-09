namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Plantilla RECURRENTE semanal: "Intermedios, martes 18:00, 60', Cancha 1".
/// Dura toda la temporada; el cambio de temporada = editar/desactivar.
///
/// El horario es la UNIDAD: tiene su nombre, su cupo y su lista de alumnos
/// (<see cref="Alumnos"/>). Una clase particular no es un caso especial, es un
/// horario con cupo 1 — antes había dos conceptos (grupo XOR alumno) para lo mismo.
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

    /// <summary>
    /// Valor hora del profe SOLO para esta clase (override del base de su membresía,
    /// para el caso "menores" que se paga menos). null = usar el valor hora base del
    /// empleado. Es el dato con el que se liquida el sueldo (ver IPoliticaDeSueldo).
    /// </summary>
    public decimal? ValorHoraProfe { get; set; }

    // ── Quiénes toman esta clase ──

    /// <summary>
    /// Cómo se llama la clase ("Intermedios"). Opcional: si está vacío, el título se
    /// arma solo con el roster (el nombre del alumno, o "Grupo de N").
    /// </summary>
    public string? Nombre { get; set; }

    /// <summary>Cuántos alumnos entran; null = sin límite. Una clase particular es cupo 1.</summary>
    public int? CupoMaximo { get; set; }

    /// <summary>Categoría sugerida, para que el portal ofrezca clases parejas. Opcional.</summary>
    public CategoriaAlumno? Categoria { get; set; }

    /// <summary>El roster: quiénes toman esta clase (con su historia de alta y baja).</summary>
    public ICollection<AlumnoHorario> Alumnos { get; set; } = new List<AlumnoHorario>();

    // ── Recurrencia semanal ──
    public DayOfWeek Dia { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public int DuracionMinutos { get; set; } = 60;

    public bool Activo { get; set; } = true;
    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;

    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
}
