using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Armador de <see cref="TurnoAgenda"/> para los tests. El repositorio devuelve un
/// record proyectado (y no la entidad) porque traer el roster entero multiplicaba las
/// filas de la consulta; como el record tiene muchos campos y casi todos los tests
/// miran dos o tres, se arma con valores por defecto y se nombra solo lo que importa.
/// </summary>
internal static class TurnosDePrueba
{
    public static TurnoAgenda Agenda(
        Guid? id = null,
        DateOnly? fecha = null,
        TimeOnly? hora = null,
        int duracion = 60,
        EstadoTurno estado = EstadoTurno.Programado,
        string? canceladoMotivo = null,
        Guid? canchaId = null,
        string cancha = "Cancha 1",
        string sede = "Club",
        Guid? horarioId = null,
        string? horarioNombre = null,
        Guid? profesorUserId = null,
        decimal? valorHoraProfe = null,
        DayOfWeek? horarioDia = null,
        TimeOnly? horarioHoraInicio = null,
        int miembrosActivos = 0,
        string? unicoMiembro = null,
        IReadOnlyList<ParticipanteAgenda>? participantes = null) =>
        new(
            id ?? Guid.NewGuid(),
            fecha ?? new DateOnly(2026, 7, 14),
            hora ?? new TimeOnly(18, 0),
            duracion,
            estado,
            canceladoMotivo,
            canchaId ?? Guid.NewGuid(),
            cancha,
            sede,
            horarioId,
            horarioNombre,
            profesorUserId,
            valorHoraProfe,
            horarioDia,
            horarioHoraInicio,
            miembrosActivos,
            unicoMiembro,
            participantes ?? []);

    public static ParticipanteAgenda Participante(
        Guid alumnoId, string nombre = "Juan", string apellido = "Pérez", bool presente = false) =>
        new(alumnoId, nombre, apellido, presente);
}
