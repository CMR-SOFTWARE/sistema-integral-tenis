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

/// <summary>El alumno pide un servicio del catálogo (solo el id; el precio es snapshot del server).</summary>
public class CrearPedidoDto
{
    [Required]
    public Guid ServicioId { get; set; }
}

/// <summary>Un pedido (visto por el profe o por el alumno).</summary>
public class PedidoDto
{
    public Guid Id { get; set; }
    public Guid AlumnoId { get; set; }
    public string AlumnoNombre { get; set; } = string.Empty;
    public string NombreServicio { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    /// <summary>Pendiente | Aceptado | Rechazado.</summary>
    public string Estado { get; set; } = string.Empty;
    public DateTime PedidoEl { get; set; }
    public DateTime? ResueltoEl { get; set; }
}
