namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Conteo de alumnos de una categoría (para el ranking con barras).</summary>
public class CategoriaConteoDto
{
    public string Categoria { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

/// <summary>
/// Resumen del dashboard del profesor. Todo sale de datos reales del tenant;
/// lo que aún no tiene vertical (clases, cuotas, cancelaciones) no viene acá.
/// </summary>
public class DashboardResumenDto
{
    public int AlumnosActivos { get; set; }
    public int NuevosEsteMes { get; set; }
    public int Pausados { get; set; }

    /// <summary>Suma de aranceles de alumnos activos (estimado, hasta que exista Cuotas).</summary>
    public decimal IngresoMensualEstimado { get; set; }

    /// <summary>Alumnos por categoría (excluye dados de baja), orden 1ra → 7ma.</summary>
    public List<CategoriaConteoDto> PorCategoria { get; set; } = [];
}
