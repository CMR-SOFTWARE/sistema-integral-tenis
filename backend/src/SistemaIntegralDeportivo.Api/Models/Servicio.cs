namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Un servicio/producto que el profe OFRECE, con precio precargado (encordado,
/// tubo de pelotas, etc.). Cada profe arma su propio catálogo — no todos
/// ofrecen lo mismo. El alumno pide desde acá (M4). Baja LÓGICA (Activo=false):
/// desactivar uno no rompe los pedidos históricos que lo referenciaban.
/// </summary>
public class Servicio
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public required string Nombre { get; set; } // "Encordado", "Tubo de pelotas"

    /// <summary>
    /// De qué se trata: medidas, colores, qué incluye. Opcional — un encordado se explica
    /// solo, una raqueta o una remera no.
    /// </summary>
    public string? Descripcion { get; set; }

    public decimal Precio { get; set; }
    public bool Activo { get; set; } = true;

    /// <summary>Las fotos del producto; la primera es la que se ve en el listado del Shop.</summary>
    public ICollection<FotoServicio> Fotos { get; set; } = [];

    public DateTime CreadoEl { get; set; } = DateTime.UtcNow;
}
