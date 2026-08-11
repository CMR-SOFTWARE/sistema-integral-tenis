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
    public string? Dni { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    /// <summary>Menor de edad (lo marca el profe): dispara la regla de tutor.</summary>
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

    /// <summary>
    /// Cuenta a la que pertenece la ficha (= UserId del titular). Las fichas que
    /// comparten FamiliaId son una familia (el front las agrupa). Null si no tiene login.
    /// </summary>
    public Guid? FamiliaId { get; set; }

    /// <summary>Foto de perfil (data URL) que cargó el alumno, o null.</summary>
    public string? FotoUrl { get; set; }

    /// <summary>Profe de cabecera (dueño o staff); null = sin asignar. El front mapea el nombre.</summary>
    public Guid? ProfesorUserId { get; set; }

    /// <summary>Club (sede) de la ficha, heredado del profe de cabecera; null = sin club.</summary>
    public Guid? SedeId { get; set; }
    public string? SedeNombre { get; set; }

    /// <summary>
    /// Tiene al menos una clase asignada. Es lo que separa a un alumno de alguien que
    /// todavía está esperando: no hay columna que lo diga, se deriva del roster.
    /// </summary>
    public bool TieneClase { get; set; }

    /// <summary>
    /// El profe lo anotó a mano en la lista de espera (le pidió otra clase hablando).
    /// Solo del lado del profe: al portal del alumno no viaja.
    /// </summary>
    public bool EnEspera { get; set; }
}

/// <summary>Un horario asignado del alumno, para la ficha del profe (día/hora/cancha).</summary>
public class AlumnoHorarioDto
{
    public string Dia { get; set; } = string.Empty;        // "Lunes", "Martes"...
    public string HoraInicio { get; set; } = string.Empty; // "18:00"
    public int DuracionMinutos { get; set; }
    public string Cancha { get; set; } = string.Empty;
    public string Sede { get; set; } = string.Empty;
    /// <summary>Cómo se llama la clase (el nombre que le puso el profe, o el automático).</summary>
    public string Titulo { get; set; } = string.Empty;
    /// <summary>Con cuántos la comparte (0 = la tiene para él solo).</summary>
    public int Companeros { get; set; }
}

/// <summary>La cuenta corriente del alumno vista desde su ficha (deuda + últimos cargos).</summary>
public class AlumnoCuentaDto
{
    /// <summary>Tiene la cuota vencida (pasó el día 10 sin pagar).</summary>
    public bool DeudaVencida { get; set; }
    /// <summary>Total impago (todos los cargos sin pagar, no solo los recientes).</summary>
    public decimal TotalAdeudado { get; set; }
    /// <summary>Últimos movimientos (para mostrar en la ficha).</summary>
    public List<CargoResumenDto> Cargos { get; set; } = [];
}

/// <summary>Línea resumida de un cargo (para la ficha del alumno).</summary>
public class CargoResumenDto
{
    public Guid Id { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateOnly Fecha { get; set; }
    public bool Pagado { get; set; }
    /// <summary>El alumno avisó que transfirió; el profe todavía no confirmó.</summary>
    public bool PagoInformado { get; set; }
    public string? MedioPago { get; set; }
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
