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
