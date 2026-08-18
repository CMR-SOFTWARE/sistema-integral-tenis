using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Cómo ve el alumno una franja de la grilla de Reservar.</summary>
public enum EstadoSlot
{
    /// <summary>Tiene lugar y es de su categoría: la puede pedir.</summary>
    Disponible,
    /// <summary>No la puede pedir. Llena o de otra categoría se ven IGUAL, a propósito.</summary>
    Ocupado,
    /// <summary>Ya viene a esta clase.</summary>
    Mia,
}

/// <summary>
/// Una franja de la grilla de Reservar del portal.
///
/// Lo que NO lleva es la parte importante: ni título de clase, ni cancha, ni precio, ni
/// cuánta gente hay adentro. El título de una clase de una sola persona ES el nombre de
/// esa persona (<c>HorarioService.TituloDe</c>), así que mandarlo le mostraba a cada
/// alumno quiénes toman clase en el club y a qué hora. Se corta en el borde, no en el
/// front: lo que no viaja no se filtra.
/// </summary>
public class SlotReservaDto
{
    /// <summary>
    /// Solo cuando es <see cref="EstadoSlot.Disponible"/>: sin el id, una franja ocupada
    /// no se puede pedir ni tocando la API a mano (el service igual revalida).
    /// </summary>
    public Guid? HorarioId { get; set; }

    /// <summary>Disponible | Ocupado | Mia.</summary>
    public string Estado { get; set; } = string.Empty;

    public string Dia { get; set; } = string.Empty; // "Tuesday" → el front lo traduce
    public TimeOnly HoraInicio { get; set; }
    public int DuracionMinutos { get; set; }

    /// <summary>El club de la franja (el suyo, salvo que la ficha no tenga club asignado).</summary>
    public string Sede { get; set; } = string.Empty;

    /// <summary>La categoría de la CLASE, para que sepa si es su nivel. Solo si es Disponible.</summary>
    public string? Categoria { get; set; }

    /// <summary>
    /// Cuántos lugares quedan. Reemplaza al "2/4": dice lo que el alumno necesita sin
    /// decir cuánta gente hay adentro. Null = sin límite de cupo, o la franja no es
    /// Disponible.
    /// </summary>
    public int? LugaresLibres { get; set; }

    /// <summary>Ya mandó un pedido para esta clase (el front deshabilita el botón).</summary>
    public bool SolicitudPendiente { get; set; }
}

/// <summary>El alumno pide un lugar en una clase (solo el id).</summary>
public class SolicitarCupoDto
{
    [Required]
    public Guid HorarioId { get; set; }
}

/// <summary>
/// Un pedido de lugar en una clase, visto por el PROFE: necesita saber quién pide y a qué
/// clase. Al alumno se le manda <see cref="MiSolicitudCupoDto"/>, que no lleva nombres.
/// </summary>
public class SolicitudCupoDto
{
    public Guid Id { get; set; }
    public Guid AlumnoId { get; set; }
    public string AlumnoNombre { get; set; } = string.Empty;
    public Guid HorarioId { get; set; }
    public string ClaseNombre { get; set; } = string.Empty;
    /// <summary>Pendiente | Aceptada | Rechazada.</summary>
    public string Estado { get; set; } = string.Empty;
    public DateTime CreadoEl { get; set; }
    public DateTime? ResueltoEl { get; set; }
}

/// <summary>
/// Mi pedido de lugar, visto desde el PORTAL. La clase se identifica por cuándo es, no
/// por su nombre: el nombre puede ser el de otro alumno (ver <see cref="SlotReservaDto"/>).
/// </summary>
public class MiSolicitudCupoDto
{
    public Guid Id { get; set; }
    public string Dia { get; set; } = string.Empty;
    public TimeOnly HoraInicio { get; set; }
    public int DuracionMinutos { get; set; }
    public string Sede { get; set; } = string.Empty;
    /// <summary>Pendiente | Aceptada | Rechazada.</summary>
    public string Estado { get; set; } = string.Empty;
    public DateTime CreadoEl { get; set; }
    public DateTime? ResueltoEl { get; set; }
}

// ── Clase individual fija (M5b) ──

/// <summary>El alumno propone una clase individual fija (elige SEDE; el profe elige la cancha al aceptar).</summary>
public class SolicitarHorarioDto
{
    [Required]
    public Guid SedeId { get; set; }

    [Required]
    public DayOfWeek Dia { get; set; }

    [Required]
    public TimeOnly HoraInicio { get; set; }

    [Range(30, 180)]
    public int DuracionMinutos { get; set; } = 60;
}

/// <summary>Una sede del club (para que el alumno elija dónde quiere la clase).</summary>
public class SedeReservaDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

/// <summary>El profe acepta una solicitud individual eligiendo una cancha libre.</summary>
public class AceptarHorarioDto
{
    [Required]
    public Guid CanchaId { get; set; }
}

/// <summary>Una cancha libre a un día/hora (para el dropdown del profe al aceptar).</summary>
public class CanchaLibreDto
{
    public Guid CanchaId { get; set; }
    public string Cancha { get; set; } = string.Empty;
    public string Sede { get; set; } = string.Empty;
}

/// <summary>Una solicitud de clase individual (vista por el profe o el alumno).</summary>
public class SolicitudHorarioDto
{
    public Guid Id { get; set; }
    public Guid AlumnoId { get; set; }
    public string AlumnoNombre { get; set; } = string.Empty;
    public string Dia { get; set; } = string.Empty; // "Tuesday" → el front traduce
    public TimeOnly HoraInicio { get; set; }
    public int DuracionMinutos { get; set; }
    /// <summary>La sede que eligió el alumno.</summary>
    public string Sede { get; set; } = string.Empty;
    /// <summary>Pendiente | Aceptada | Rechazada.</summary>
    public string Estado { get; set; } = string.Empty;
    /// <summary>Cancha asignada (solo si Aceptada).</summary>
    public string? Cancha { get; set; }
    public DateTime CreadoEl { get; set; }
    public DateTime? ResueltoEl { get; set; }
}

/// <summary>
/// Si hay lugar para una clase individual a un día/hora (chequea TODAS las
/// canchas del club). El alumno lo ve en vivo al armar el pedido.
/// </summary>
public class DisponibilidadDto
{
    public bool HayLugar { get; set; }
    public int CanchasLibres { get; set; }
}

// ── Clase suelta (M5c) ──

/// <summary>El alumno reserva una clase suelta (individual, una fecha puntual).</summary>
public class SolicitarClaseSueltaDto
{
    [Required]
    public Guid SedeId { get; set; }

    [Required]
    public DateOnly Fecha { get; set; }

    [Required]
    public TimeOnly HoraInicio { get; set; }

    [Range(30, 180)]
    public int DuracionMinutos { get; set; } = 60;
}

/// <summary>El profe confirma una clase suelta eligiendo una cancha libre.</summary>
public class ConfirmarClaseSueltaDto
{
    [Required]
    public Guid CanchaId { get; set; }
}

/// <summary>
/// El PROFE asigna una clase suelta (no la pide el alumno): elige a quién, cuándo, con
/// qué profe, y si le genera el cargo o es una clase de prueba.
/// </summary>
public class AsignarClaseSueltaDto
{
    [Required]
    public Guid AlumnoId { get; set; }

    [Required]
    public Guid CanchaId { get; set; }

    [Required]
    public DateOnly Fecha { get; set; }

    [Required]
    public TimeOnly HoraInicio { get; set; }

    [Range(15, 240)]
    public int DuracionMinutos { get; set; } = 60;

    /// <summary>Profe a cargo; null = sin asignar (no le cuenta a nadie para el sueldo).</summary>
    public Guid? ProfesorUserId { get; set; }

    /// <summary>
    /// false = **clase de prueba**: no se le cobra nada. true = se le carga el valor de
    /// clase individual de Configuración, prorrateado por la duración.
    /// </summary>
    public bool GeneraCargo { get; set; } = true;
}

/// <summary>
/// Reprograma una clase suelta ya asignada: solo lo agendable (fecha, hora, cancha,
/// duración, profe). No incluye alumno ni cobro: eso no se toca al editar.
/// </summary>
public class EditarClaseSueltaDto
{
    [Required]
    public Guid CanchaId { get; set; }

    [Required]
    public DateOnly Fecha { get; set; }

    [Required]
    public TimeOnly HoraInicio { get; set; }

    [Range(15, 240)]
    public int DuracionMinutos { get; set; } = 60;

    public Guid? ProfesorUserId { get; set; }
}

/// <summary>Una clase suelta (vista por el profe o el alumno).</summary>
public class ClaseSueltaDto
{
    public Guid Id { get; set; }
    public Guid AlumnoId { get; set; }
    public string AlumnoNombre { get; set; } = string.Empty;
    public string Sede { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public int DuracionMinutos { get; set; }
    public decimal Monto { get; set; }
    /// <summary>Pendiente | Confirmada | Rechazada.</summary>
    public string Estado { get; set; } = string.Empty;
    /// <summary>El alumno avisó que pagó (pendiente de que el profe confirme).</summary>
    public bool PagoInformado { get; set; }
    public bool Pagado { get; set; }
    /// <summary>Cancha asignada (solo si Confirmada).</summary>
    public string? Cancha { get; set; }
    public DateTime CreadoEl { get; set; }
    public DateTime? ResueltoEl { get; set; }
}
