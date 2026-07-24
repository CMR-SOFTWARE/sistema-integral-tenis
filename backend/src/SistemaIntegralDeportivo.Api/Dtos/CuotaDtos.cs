using System.ComponentModel.DataAnnotations;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Alta manual de un cargo: Producto (encordado, pelotas) o Ajuste (+/-).</summary>
public class CreateCargoManualDto
{
    [Required]
    public Guid AlumnoId { get; set; }

    [Required]
    public TipoCargo Tipo { get; set; } // Producto o Ajuste (Clase es automática)

    [Required, StringLength(120)]
    public string Concepto { get; set; } = string.Empty;

    [Required]
    public decimal Monto { get; set; } // Ajuste puede ser negativo

    public DateOnly? Fecha { get; set; } // default: hoy
}

public class PagarCargoDto
{
    [Required]
    public MedioPago Medio { get; set; }
}

public class PagarMesDto
{
    [Required]
    public Guid AlumnoId { get; set; }

    [Required]
    public MedioPago Medio { get; set; }
}

/// <summary>El profe rechaza el pago informado del mes de un alumno (no lleva medio).</summary>
public class RechazarPagoMesDto
{
    [Required]
    public Guid AlumnoId { get; set; }
}

/// <summary>Una línea de la cuenta corriente.</summary>
public class CargoResponseDto
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Concepto { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateOnly Fecha { get; set; }
    public bool Pagado { get; set; }
    public DateTime? PagadoEl { get; set; }
    public string? MedioPago { get; set; }
    /// <summary>El alumno avisó que transfirió, pendiente de que el profe confirme.</summary>
    public bool PagoInformado { get; set; }
    public DateTime? PagoInformadoEl { get; set; }
}

/// <summary>La cuenta del mes de un alumno: cargos + totales + estado calculado.</summary>
public class AlumnoLiquidacionDto
{
    public Guid AlumnoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    /// <summary>Cuenta del alumno (= titular). Las filas con el mismo FamiliaId son una familia.</summary>
    public Guid? FamiliaId { get; set; }
    public string Modalidad { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal Pagado { get; set; }
    public decimal Saldo { get; set; }
    /// <summary>
    /// Pagada | Informado | Pendiente | Vencida (calculado, nunca guardado).
    /// "Informado" = debe, pero avisó que transfirió todo y espera confirmación.
    /// </summary>
    public string Estado { get; set; } = string.Empty;
    public List<CargoResponseDto> Cargos { get; set; } = [];
}

/// <summary>
/// La cuota CONSOLIDADA de una familia (Capa 2b): la liquidación de cada miembro
/// + los totales. El titular ve un solo número y puede informar todo junto.
/// </summary>
public class CuotaFamiliaDto
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public List<AlumnoLiquidacionDto> Miembros { get; set; } = [];
    public decimal Total { get; set; }
    public decimal Pagado { get; set; }
    public decimal Saldo { get; set; }
    /// <summary>Algún miembro debe y no informó todavía → habilita "Ya transferí" familiar.</summary>
    public bool PuedeInformar { get; set; }
}

/// <summary>La pantalla Cuotas de un mes: liquidaciones por alumno + stats.</summary>
public class LiquidacionMesDto
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal TotalFacturado { get; set; }
    public decimal TotalCobrado { get; set; }
    public decimal TotalPendiente { get; set; }
    public int AlumnosVencidos { get; set; }
    public List<AlumnoLiquidacionDto> Liquidaciones { get; set; } = [];
}
