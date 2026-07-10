using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class TurnoService : ITurnoService
{
    private readonly ITurnoRepository _turnos;
    private readonly IHorarioRepository _horarios;
    private readonly IGrupoRepository _grupos;

    public TurnoService(ITurnoRepository turnos, IHorarioRepository horarios, IGrupoRepository grupos)
    {
        _turnos = turnos;
        _horarios = horarios;
        _grupos = grupos;
    }

    public async Task<IReadOnlyList<TurnoResponseDto>> ObtenerSemanaAsync(
        DateOnly lunes, CancellationToken ct = default)
    {
        var domingo = lunes.AddDays(6);

        // Generación perezosa: materializar lo que falte de esta semana
        var horarios = await _horarios.ListarActivosAsync(ct);
        var generoAlguno = false;

        foreach (var horario in horarios)
        {
            // Fecha concreta del día del horario dentro de esta semana
            var offset = ((int)horario.Dia - (int)DayOfWeek.Monday + 7) % 7;
            var fecha = lunes.AddDays(offset);

            // Idempotencia: si ya existe el turno de esa fecha, no se toca
            // (refuerzo extra: índice único HorarioId+Fecha en la base)
            var yaGeneradas = await _turnos.FechasGeneradasAsync(horario.Id, lunes, domingo, ct);
            if (yaGeneradas.Contains(fecha)) continue;

            var turno = new Turno
            {
                HorarioId = horario.Id,
                CanchaId = horario.CanchaId,
                Fecha = fecha,
                HoraInicio = horario.HoraInicio,
                DuracionMinutos = horario.DuracionMinutos,
                // TenantId lo asigna el repositorio
            };

            // Roster CONGELADO al generar (fija el divisor del precio):
            // grupal → miembros activos del grupo; individual → ese alumno
            if (horario.GrupoId is not null)
            {
                var grupo = await _grupos.ObtenerAsync(horario.GrupoId.Value, ct);
                if (grupo is not null)
                {
                    foreach (var m in grupo.Alumnos.Where(x => x.FechaBaja is null))
                        turno.Participantes.Add(new TurnoParticipante { Turno = turno, AlumnoId = m.AlumnoId });
                }
            }
            else if (horario.AlumnoId is not null)
            {
                turno.Participantes.Add(new TurnoParticipante { Turno = turno, AlumnoId = horario.AlumnoId.Value });
            }

            await _turnos.AgregarAsync(turno, ct);
            generoAlguno = true;
        }

        if (generoAlguno)
            await _turnos.GuardarCambiosAsync(ct);

        var turnosSemana = await _turnos.ListarEntreAsync(lunes, domingo, ct);
        return turnosSemana
            .OrderBy(t => t.Fecha).ThenBy(t => t.HoraInicio)
            .Select(Mapear)
            .ToList();
    }

    public async Task MarcarAsistenciaAsync(
        Guid turnoId, Guid alumnoId, bool presente, CancellationToken ct = default)
    {
        var turno = await _turnos.ObtenerAsync(turnoId, ct)
            ?? throw new ReglaDeNegocioException("El turno no existe.");

        var participante = turno.Participantes.FirstOrDefault(p => p.AlumnoId == alumnoId)
            ?? throw new ReglaDeNegocioException("El alumno no participa de este turno.");

        // Solo registro: la asistencia NO afecta cargos (modelo-precios.md)
        participante.Presente = presente;
        await _turnos.GuardarCambiosAsync(ct);
    }

    public async Task CancelarAsync(Guid turnoId, string motivo, CancellationToken ct = default)
    {
        var turno = await _turnos.ObtenerAsync(turnoId, ct)
            ?? throw new ReglaDeNegocioException("El turno no existe.");

        if (turno.Estado == EstadoTurno.Cancelado)
            throw new ReglaDeNegocioException("El turno ya está cancelado.");

        // Nunca se borra: queda motivo y cuándo (historia)
        turno.Estado = EstadoTurno.Cancelado;
        turno.CanceladoMotivo = motivo;
        turno.CanceladoEl = DateTime.UtcNow;
        await _turnos.GuardarCambiosAsync(ct);
    }

    private static TurnoResponseDto Mapear(Turno t) => new()
    {
        Id = t.Id,
        Fecha = t.Fecha,
        HoraInicio = t.HoraInicio,
        DuracionMinutos = t.DuracionMinutos,
        Estado = t.Estado.ToString(),
        CanceladoMotivo = t.CanceladoMotivo,
        Titulo = t.Horario?.Grupo?.Nombre
            ?? (t.Horario?.Alumno is not null
                ? $"{t.Horario.Alumno.Nombre} {t.Horario.Alumno.Apellido} (individual)"
                : string.Empty),
        Cancha = t.Cancha?.Nombre ?? string.Empty,
        Sede = t.Cancha?.Sede?.Nombre ?? string.Empty,
        Participantes = t.Participantes.Select(p => new ParticipanteTurnoDto
        {
            AlumnoId = p.AlumnoId,
            Nombre = p.Alumno?.Nombre ?? string.Empty,
            Apellido = p.Alumno?.Apellido ?? string.Empty,
            Presente = p.Presente,
        }).ToList(),
    };
}
