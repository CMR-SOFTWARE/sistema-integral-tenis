namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>
/// Borde de salida: la forma que la API expone de un alumno. Nunca se
/// devuelve la entidad EF cruda. Los enums salen como texto y EsMenor
/// se calcula (no existe como columna).
/// </summary>
public class AlumnoResponseDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public bool EsMenor { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public decimal? Arancel { get; set; }
    public string? Notas { get; set; }
    public Guid? TutorId { get; set; }
    public DateTime CreadoEl { get; set; }
    /// <summary>Señal de morosidad (cuota vencida = pasó el día 10 sin pagar).</summary>
    public bool DeudaVencida { get; set; }
}
