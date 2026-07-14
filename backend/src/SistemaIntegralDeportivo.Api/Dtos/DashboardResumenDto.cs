namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Conteo de alumnos de una categoría (para el ranking con barras).</summary>
public class CategoriaConteoDto
{
    public string Categoria { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

/// <summary>Una clase de HOY para la tarjeta "Próximas clases".</summary>
public class ClaseHoyDto
{
    public Guid TurnoId { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public int DuracionMinutos { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Cancha { get; set; } = string.Empty;
    public int Participantes { get; set; }
    public string Estado { get; set; } = string.Empty;
}

/// <summary>
/// Resumen de cuotas del mes en curso, sobre cargos YA materializados
/// (el dashboard no genera liquidación: eso pasa al entrar a Cuotas).
/// </summary>
public class CuotasPendientesDto
{
    public int AlumnosPendientes { get; set; }
    public int AlumnosVencidos { get; set; }
    public decimal TotalPendiente { get; set; }
}

/// <summary>Resumen del dashboard del profesor. Todo sale de datos reales del tenant.</summary>
public class DashboardResumenDto
{
    public int AlumnosActivos { get; set; }
    public int NuevosEsteMes { get; set; }
    public int Pausados { get; set; }

    /// <summary>Cargos del mes en curso ya pagados (misma definición que "Total cobrado" de Cuotas).</summary>
    public decimal RecaudacionDelMes { get; set; }

    /// <summary>Alumnos por categoría (excluye dados de baja), orden 1ra → 7ma.</summary>
    public List<CategoriaConteoDto> PorCategoria { get; set; } = [];

    /// <summary>Turnos de hoy ordenados por hora (incluye los cancelados, marcados).</summary>
    public List<ClaseHoyDto> ClasesHoy { get; set; } = [];

    public CuotasPendientesDto CuotasPendientes { get; set; } = new();

    /// <summary>Últimas cancelaciones (turnos enteros + avisos de alumnos), la más nueva primero.</summary>
    public List<CancelacionDto> CancelacionesRecientes { get; set; } = [];
}
