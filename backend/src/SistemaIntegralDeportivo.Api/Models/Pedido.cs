namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Un pedido de servicio del alumno (M4): "quiero un encordado". El
/// Nombre/Precio son SNAPSHOT del servicio al momento de pedir (si el profe
/// cambia el precio después, el pedido conserva lo que el alumno vio — misma
/// filosofía que el Cargo). El profe lo ACEPTA (nace el Cargo, CargoId) o lo
/// RECHAZA. La deuda no existe hasta que se acepta.
/// </summary>
public class Pedido
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    /// <summary>El servicio pedido (referencia; el nombre/precio van como snapshot).</summary>
    public Guid ServicioId { get; set; }
    public Servicio? Servicio { get; set; }

    // ── Snapshot al momento de pedir ──
    public required string NombreServicio { get; set; }
    public decimal Precio { get; set; }

    public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;

    public DateTime PedidoEl { get; set; } = DateTime.UtcNow;
    /// <summary>Cuándo el profe lo aceptó o rechazó (null mientras está Pendiente).</summary>
    public DateTime? ResueltoEl { get; set; }

    /// <summary>El cargo que nació al aceptarlo (null si Pendiente o Rechazado).</summary>
    public Guid? CargoId { get; set; }
    public Cargo? Cargo { get; set; }
}
