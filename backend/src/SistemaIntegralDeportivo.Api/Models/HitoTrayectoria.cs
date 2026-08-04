namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Un hito de la carrera del profe ("2010 — Profesor Nacional"). Va como tabla y no
/// como un textarea suelto para poder mostrarlo como línea de tiempo y dejar que el
/// profe edite o reordene un hito sin reescribir todo lo demás.
/// </summary>
public class HitoTrayectoria
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PerfilProfesorId { get; set; }
    public PerfilProfesor Perfil { get; set; } = null!;

    public int Anio { get; set; }

    public required string Titulo { get; set; }

    /// <summary>El detalle que amplía el título (opcional).</summary>
    public string? Detalle { get; set; }

    /// <summary>Posición en la línea de tiempo: la elige el profe, no el año.</summary>
    public int Orden { get; set; }
}
