namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Una línea de un <see cref="Pedido"/>: un servicio del carrito con su cantidad.
/// Nombre/PrecioUnitario son SNAPSHOT del servicio al momento de pedir (si el
/// profe cambia el precio después, la línea conserva lo que el alumno vio —
/// misma filosofía que el Cargo).
/// </summary>
public class PedidoLinea
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;

    /// <summary>El servicio pedido (referencia; el nombre/precio van como snapshot).</summary>
    public Guid ServicioId { get; set; }
    public Servicio? Servicio { get; set; }

    // ── Snapshot al momento de pedir ──
    public required string NombreServicio { get; set; }
    public decimal PrecioUnitario { get; set; }

    public int Cantidad { get; set; } = 1;
}
