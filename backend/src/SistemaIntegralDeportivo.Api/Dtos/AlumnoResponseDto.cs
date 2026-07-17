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
    /// <summary>Cómo liquida: Mensual (vence el 10) o PorClase (ADR-0009).</summary>
    public string Modalidad { get; set; } = string.Empty;
    public decimal? Arancel { get; set; }
    public string? Notas { get; set; }
    public Guid? TutorId { get; set; }
    public DateTime CreadoEl { get; set; }
    /// <summary>Señal de morosidad (cuota vencida = pasó el día 10 sin pagar).</summary>
    public bool DeudaVencida { get; set; }

    /// <summary>Tiene acceso al portal (para mostrar/ocultar "Crear acceso").</summary>
    public bool TieneUsuario { get; set; }
}

/// <summary>
/// Alumno ACTIVO al que se le pasó el día 15 sin pagar: el profe decide si
/// lo saca del calendario (nunca es automático).
/// </summary>
public class MorosoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    /// <summary>Total impago (todos los meses adeudados).</summary>
    public decimal Deuda { get; set; }
    /// <summary>Meses con cargos impagos, ej: "Junio, Julio".</summary>
    public string MesesAdeudados { get; set; } = string.Empty;
}
