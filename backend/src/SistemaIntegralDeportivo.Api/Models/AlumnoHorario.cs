namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Quién toma una clase fija. Es la tabla intermedia EXPLÍCITA de Alumno ↔ Horario
/// (reemplaza a <see cref="AlumnoGrupo"/>, que colgaba del grupo): explícita porque
/// guarda historia — "Pepe estuvo en los martes de 18 de marzo a junio".
/// La clave primaria compuesta (AlumnoId, HorarioId) se configura en el DbContext,
/// y por eso al reingresar se reactiva la fila en vez de crear otra.
/// </summary>
public class AlumnoHorario
{
    public Guid AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    public Guid HorarioId { get; set; }
    public Horario Horario { get; set; } = null!;

    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    /// <summary>null = sigue tomando esta clase.</summary>
    public DateTime? FechaBaja { get; set; }
}
