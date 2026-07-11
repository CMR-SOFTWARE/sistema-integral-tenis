namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Un turno visto por el ALUMNO, con su asistencia y sus compañeros.</summary>
public class MiTurnoDto
{
    public Guid Id { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public int DuracionMinutos { get; set; }
    public string Titulo { get; set; } = string.Empty;
    /// <summary>Categoría del grupo (null en clase individual) — el chip del mockup.</summary>
    public string? Categoria { get; set; }
    public string Sede { get; set; } = string.Empty;
    public string Cancha { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? CanceladoMotivo { get; set; }
    public bool Presente { get; set; }
    /// <summary>Nombres de los demás participantes ("con Mateo, Lucas").</summary>
    public List<string> Companeros { get; set; } = [];
}

/// <summary>Mis turnos: los que vienen y lo que ya pasó (historial).</summary>
public class MisTurnosDto
{
    /// <summary>Desde hoy hasta fin del mes que viene, ascendente.</summary>
    public List<MiTurnoDto> Proximos { get; set; } = [];

    /// <summary>Desde el mes pasado hasta ayer, el más reciente primero.</summary>
    public List<MiTurnoDto> Historial { get; set; } = [];
}

/// <summary>
/// Lo que el alumno puede editar de su propia ficha: SUS datos de contacto.
/// DNI, categoría, modalidad y estado los administra el profesor.
/// </summary>
public class ActualizarMiPerfilDto
{
    public required string Telefono { get; set; }
    public string? Email { get; set; }
}

/// <summary>La ficha del alumno vista por él mismo.</summary>
public class MiPerfilDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string Dni { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Modalidad { get; set; } = string.Empty;
    public string Club { get; set; } = string.Empty;
}
