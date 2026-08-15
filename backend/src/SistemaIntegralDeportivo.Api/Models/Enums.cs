namespace SistemaIntegralDeportivo.Api.Models;

public enum EstadoTenant
{
    PendientePago,
    Activo,
    Suspendido,
}

public enum TipoTenant
{
    Profesor,
    Club,
}

public enum RolTenant
{
    Dueño,
    Staff,
}

public enum EstadoAlumno
{
    Activo,
    Suspendido,
    Inactivo,
}

public enum CategoriaAlumno
{
    Primera,
    Segunda,
    Tercera,
    Cuarta,
    Quinta,
    Sexta,
    A,
    B1,
    B2,
    C1,
    C2,
    D,
    SinCategoria,
}

public enum RelacionTutor
{
    Padre,
    Madre,
    TutorLegal,
    Otro,
}

public enum ListaAlumnos
{
    Todos,
    ConClase,
    SinClase,
}

public enum EstadoTurno
{
    Programado,
    Cancelado,
}

public enum CanceladoPor
{
    Profesor,
    Alumno,
}

public enum TipoBloqueo
{
    Fijo,
    Rango,
}

public enum MotivoBloqueo
{
    MalClima,
    MotivosPersonales,
    Torneo,
    MantenimientoCancha,
}

public enum EstadoSolicitud
{
    Pendiente,
    Aprobada,
    Rechazada,
}

public enum EstadoSolicitudHorario
{
    Pendiente,
    Aceptada,
    Rechazada,
}

public enum EstadoSolicitudGrupo
{
    Pendiente,
    Aceptada,
    Rechazada,
}

public enum EstadoClaseSuelta
{
    Pendiente,
    Confirmada,
    Rechazada,
}

public enum ModalidadPago
{
    Mensual,
    PorClase,
}

public enum TipoCargo
{
    Clase,
    Producto,
    Ajuste,
    Cuota,
}

public enum MedioPago
{
    Efectivo,
    Transferencia,
    Otro,
}

public enum EstadoPedido
{
    Pendiente,
    Aceptado,
    Rechazado,
}

public enum EstadoJuegoPendiente
{
    Propuesto,
    Aceptado,
    Finalizado,
}

public enum ModalidadRanking
{
    Singles,
    Dobles,
}

/// <summary>Alcance geográfico de un snapshot oficial. Global = toda la plataforma;
/// los demás reordenan SOLO dentro de ese grupo (un #1 de ciudad puede ser rango
/// O global). TorneoAmigos queda fuera de este build (sin lógica activa).</summary>
public enum ScopeRanking
{
    Global,
    Ciudad,
    Provincia,
    Pais,
}

public enum EstadoJuegoRevision
{
    Pendiente,
    Resuelta,
}
