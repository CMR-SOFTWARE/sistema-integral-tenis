using System.ComponentModel.DataAnnotations;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Un horario que suma al sueldo del profe: sus clases del mes × valor hora.</summary>
public class SueldoHorarioDto
{
    public Guid HorarioId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Dia { get; set; } = string.Empty;
    public TimeOnly HoraInicio { get; set; }
    /// <summary>Valor hora efectivo (override del horario o base del profe); null = sin definir.</summary>
    public decimal? ValorHora { get; set; }
    public int Clases { get; set; }
    public decimal Horas { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>Lo que un empleado GANÓ en el mes (salida de IPoliticaDeSueldo, antes del pago).</summary>
public class SueldoCalculadoDto
{
    public Guid UserId { get; set; }
    public Guid MembresiaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public decimal Monto { get; set; }
    public decimal HorasTotales { get; set; }
    /// <summary>false = alguna clase quedó sin tarifa (ni override ni base) → chip "sin valor hora".</summary>
    public bool TieneValorHora { get; set; }
    public List<SueldoHorarioDto> Detalle { get; set; } = [];
}

/// <summary>La fila de un empleado en la pantalla Sueldos: lo calculado + lo pagado.</summary>
public class EmpleadoSueldoDto
{
    public Guid UserId { get; set; }
    public Guid MembresiaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public decimal Calculado { get; set; }
    public decimal Pagado { get; set; }
    public decimal Saldo { get; set; }
    /// <summary>Pagado | Pendiente (calculado, nunca guardado).</summary>
    public string Estado { get; set; } = string.Empty;
    public decimal HorasTotales { get; set; }
    public bool TieneValorHora { get; set; }
    public string? MedioPago { get; set; }
    public DateTime? PagadoEl { get; set; }
    public List<SueldoHorarioDto> Detalle { get; set; } = [];
}

/// <summary>La pantalla Sueldos de un mes: un renglón por empleado + totales.</summary>
public class LiquidacionSueldosDto
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal TotalAPagar { get; set; }
    public decimal TotalPagado { get; set; }
    public decimal TotalPendiente { get; set; }
    public List<EmpleadoSueldoDto> Empleados { get; set; } = [];
}

/// <summary>Egreso de sueldos pagado en un mes (panel/reporte).</summary>
public class SueldoMesDto
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal Pagado { get; set; }
}

/// <summary>Registrar el pago del sueldo de un empleado por un mes (monto ajustable al pagar).</summary>
public class PagarSueldoDto
{
    [Required] public Guid UserId { get; set; }
    [Required] public int Anio { get; set; }
    [Required] public int Mes { get; set; }
    [Required, Range(0, 99_999_999)] public decimal Monto { get; set; }
    [Required] public MedioPago Medio { get; set; }
}

/// <summary>Revertir el pago del sueldo de un empleado por un mes (por si se registró mal).</summary>
public class RevertirSueldoDto
{
    [Required] public Guid UserId { get; set; }
    [Required] public int Anio { get; set; }
    [Required] public int Mes { get; set; }
}

/// <summary>Setear/actualizar el valor hora BASE de un profe empleado (null = borrarlo).</summary>
public class ValorHoraStaffDto
{
    [Range(0, 99_999_999)] public decimal? ValorHora { get; set; }
}
