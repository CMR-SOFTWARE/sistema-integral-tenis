namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Una CLASE SUELTA (M5c): el alumno reserva una clase individual en una FECHA
/// puntual (no recurrente) para probar/esporádico. Al pedir se le crea el
/// Cargo (precio individual); el alumno informa el pago y el profe CONFIRMA
/// (elige cancha, nace el turno suelto, se marca pagado el cargo) o RECHAZA.
/// </summary>
public class ClaseSuelta
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    // ── Lo que pide el alumno ──
    public Guid SedeId { get; set; }
    public Sede Sede { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public int DuracionMinutos { get; set; } = 60;

    public EstadoClaseSuelta Estado { get; set; } = EstadoClaseSuelta.Pendiente;

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
    public DateTime? ResueltoEl { get; set; }

    /// <summary>
    /// El cargo que se genera al pedir (precio individual); el alumno lo paga.
    /// Nullable para poder RECHAZAR: se borra el cargo y la clase queda como
    /// historia (Rechazada) con CargoId en null.
    /// </summary>
    public Guid? CargoId { get; set; }
    public Cargo? Cargo { get; set; }

    // ── Se completan al confirmar: la cancha que eligió el profe y el turno ──
    public Guid? CanchaId { get; set; }
    public Cancha? Cancha { get; set; }
    public Guid? TurnoId { get; set; }
    public Turno? Turno { get; set; }
}
