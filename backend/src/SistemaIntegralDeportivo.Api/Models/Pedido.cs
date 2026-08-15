namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Un pedido de servicios del alumno (M4 + carrito): "quiero un encordado y dos
/// tubos de pelotas", en una o varias <see cref="PedidoLinea"/>. El profe lo
/// ACEPTA (nace un único Cargo con el total, CargoId) o lo RECHAZA. La deuda no
/// existe hasta que se acepta.
/// </summary>
public class Pedido
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    public ICollection<PedidoLinea> Lineas { get; set; } = new List<PedidoLinea>();

    public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;

    public DateTime PedidoEl { get; set; } = DateTime.UtcNow;
    /// <summary>Cuándo el profe lo aceptó o rechazó (null mientras está Pendiente).</summary>
    public DateTime? ResueltoEl { get; set; }

    /// <summary>El cargo que nació al aceptarlo (null si Pendiente o Rechazado).</summary>
    public Guid? CargoId { get; set; }
    public Cargo? Cargo { get; set; }
}
