namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Recaudación de un mes (cargos pagados, por mes del CARGO — igual que el dashboard).</summary>
public class MesRecaudacionDto
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal Total { get; set; }
}

/// <summary>Reportes del profe: recaudación de los últimos 6 meses + ranking por categoría.</summary>
public class ReportesDto
{
    /// <summary>Exactamente 6 entradas, del mes más viejo al actual (meses sin cobros = 0).</summary>
    public List<MesRecaudacionDto> RecaudacionMensual { get; set; } = [];

    /// <summary>Mismos datos que el dashboard (orden 1ra → 7ma).</summary>
    public List<CategoriaConteoDto> PorCategoria { get; set; } = [];
}
