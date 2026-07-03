namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Tabla intermedia EXPLÍCITA de la relación N:M Alumno ↔ Grupo. Es explícita
/// (y no una relación implícita) porque necesitamos guardar historia:
/// "Pepe estuvo en este grupo de marzo a junio".
/// La clave primaria compuesta (AlumnoId, GrupoId) se configura en el DbContext.
/// </summary>
public class AlumnoGrupo
{
    public Guid AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    public Guid GrupoId { get; set; }
    public Grupo Grupo { get; set; } = null!;

    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    public DateTime? FechaBaja { get; set; } // null = sigue en el grupo
}
