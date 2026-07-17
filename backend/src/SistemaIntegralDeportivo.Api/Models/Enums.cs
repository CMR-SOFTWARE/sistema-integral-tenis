namespace SistemaIntegralDeportivo.Api.Models;

// Enums del dominio. En la base se guardan como texto (lo configuramos
// en el DbContext con HasConversion<string>()) para que sean legibles
// al inspeccionar el .db, en vez de números 0,1,2...

/// <summary>Tipo de cliente dueño de los datos (multi-tenant).</summary>
public enum TipoTenant
{
    Profesor,
    Club // Fase 2, pero el enum ya lo soporta
}

/// <summary>
/// Ciclo de vida del negocio: nace PendientePago al registrarse el profesor
/// y pasa a Activo cuando paga la suscripción (hoy simulado; Mercado Pago al
/// desplegar). Suspendido: dejó de pagar (fase futura).
/// </summary>
public enum EstadoTenant
{
    PendientePago,
    Activo,
    Suspendido
}

/// <summary>Categoría deportiva del alumno (7ma = inicial, 1ra = la mejor).</summary>
public enum CategoriaAlumno
{
    Primera,
    Segunda,
    Tercera,
    Cuarta,
    Quinta,
    Sexta,
    Septima,
    SinCategoria // alumno nuevo, todavía no evaluado
}

/// <summary>Ciclo de vida del alumno. Nunca se borra: se suspende o inactiva.</summary>
public enum EstadoAlumno
{
    Activo,     // al día, puede reservar
    Suspendido, // no pagó → se bloquea reserva, NO se borra
    Inactivo    // dejó de venir, se conserva el historial
}

/// <summary>Vínculo del tutor con el alumno menor.</summary>
public enum RelacionTutor
{
    Padre,
    Madre,
    TutorLegal,
    Otro
}

/// <summary>Estado del turno concreto. Cancelado conserva motivo y fecha; nunca se borra.</summary>
public enum EstadoTurno
{
    Programado,
    Cancelado
}

/// <summary>Quién canceló el turno ENTERO (el aviso individual del alumno vive en TurnoParticipante).</summary>
public enum CanceladoPor
{
    Profesor,
    Alumno
}

/// <summary>Forma del bloqueo de agenda: recurrente semanal o fecha puntual.</summary>
public enum TipoBloqueo
{
    Fijo, // se repite todas las semanas (día + franja horaria)
    Rango // una fecha concreta, con motivo
}

/// <summary>Por qué se bloquea una fecha (solo bloqueos por rango).</summary>
public enum MotivoBloqueo
{
    MalClima,
    MotivosPersonales,
    Torneo,
    MantenimientoCancha
}

/// <summary>Ciclo de una solicitud de alumno a un club.</summary>
public enum EstadoSolicitud
{
    Pendiente,
    Aprobada,
    Rechazada
}

/// <summary>Tipo de línea en la cuenta corriente del alumno (ADR-0009).</summary>
public enum TipoCargo
{
    Clase,    // auto, desde un turno (grupal ÷ asignados o individual entera)
    Producto, // manual: encordado, tubo de pelotas, etc.
    Ajuste    // manual, monto + o - con motivo (hermanos, beca, redondeo)
}

/// <summary>Cómo se registró un pago.</summary>
public enum MedioPago
{
    Efectivo,
    Transferencia,
    Otro
}

/// <summary>
/// Ciclo de un pedido de servicio (M4): el alumno lo pide (Pendiente), el
/// profe lo acepta (nace el cargo) o lo rechaza. La deuda recién existe si
/// el profe acepta — la cuenta corriente solo tiene deudas reales.
/// </summary>
public enum EstadoPedido
{
    Pendiente,
    Aceptado,
    Rechazado
}

/// <summary>Cómo liquida el alumno: el mes entero (vence el 10) o cargo por cargo.</summary>
public enum ModalidadPago
{
    Mensual,
    PorClase
}
