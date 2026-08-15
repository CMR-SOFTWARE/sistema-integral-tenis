using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Un servicio del catálogo del profe (encordado, tubo, etc.).</summary>
public class ServicioDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public bool Activo { get; set; }
}

/// <summary>Alta/edición de un servicio del catálogo.</summary>
public class GuardarServicioDto
{
    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Range(0, 10_000_000)]
    public decimal Precio { get; set; }
}

/// <summary>Baja/reactivación de un servicio del catálogo.</summary>
public class CambiarActivoDto
{
    public bool Activo { get; set; }
}

/// <summary>Una línea del carrito: qué servicio y cuántos (el precio es snapshot del server).</summary>
public class LineaCarritoDto
{
    [Required]
    public Guid ServicioId { get; set; }

    [Range(1, 99)]
    public int Cantidad { get; set; } = 1;
}

/// <summary>El alumno pide su carrito: una o varias líneas, en un solo pedido.</summary>
public class CrearPedidoDto
{
    [Required, MinLength(1)]
    public List<LineaCarritoDto> Lineas { get; set; } = [];
}

/// <summary>Una línea de un pedido, con el snapshot ya resuelto.</summary>
public class PedidoLineaDto
{
    public Guid ServicioId { get; set; }
    public string NombreServicio { get; set; } = string.Empty;
    public decimal PrecioUnitario { get; set; }
    public int Cantidad { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>Un pedido (visto por el profe o por el alumno), con todas sus líneas.</summary>
public class PedidoDto
{
    public Guid Id { get; set; }
    public Guid AlumnoId { get; set; }
    public string AlumnoNombre { get; set; } = string.Empty;
    public List<PedidoLineaDto> Lineas { get; set; } = [];
    public decimal Total { get; set; }
    /// <summary>Pendiente | Aceptado | Rechazado.</summary>
    public string Estado { get; set; } = string.Empty;
    public DateTime PedidoEl { get; set; }
    public DateTime? ResueltoEl { get; set; }
}
