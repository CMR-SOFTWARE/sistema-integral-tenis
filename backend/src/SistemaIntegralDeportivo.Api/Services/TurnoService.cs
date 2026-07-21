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
    private readonly ICargoRepository _cargos;
    private readonly IBloqueoRepository _bloqueos;
    private readonly IUsuarioActual _usuario;

    public TurnoService(
        ITurnoRepository turnos, IHorarioRepository horarios, IGrupoRepository grupos,
        ICargoRepository cargos, IBloqueoRepository bloqueos, IUsuarioActual usuario)
    {
        _turnos = turnos;
        _horarios = horarios;
        _grupos = grupos;
        _cargos = cargos;
        _bloqueos = bloqueos;
        _usuario = usuario;
    }

    public async Task<IReadOnlyList<TurnoResponseDto>> ObtenerSemanaAsync(
        DateOnly lunes, CancellationToken ct = default)
    {
        var domingo = lunes.AddDays(6);

        // Generación perezosa: materializar lo que falte de esta semana
        if (await GenerarFaltantesAsync(lunes, domingo, ct))
            await _turnos.GuardarCambiosAsync(ct);

        var turnosSemana = await _turnos.ListarEntreAsync(lunes, domingo, ct);

        // El profe EMPLEADO ve solo SUS clases (las de horarios que tiene asignados);
        // el dueño ve todas. Los turnos sin horario (clases sueltas) no son de nadie.
        if (_usuario.EsStaff)
            turnosSemana = turnosSemana
                .Where(t => t.Horario?.ProfesorUserId == _usuario.UserId)
                .ToList();

        var deudores = await DeudoresDeAsync(turnosSemana, ct);
        return turnosSemana
            .OrderBy(t => t.Fecha).ThenBy(t => t.HoraInicio)
            .Select(t => Mapear(t, deudores))
            .ToList();
    }

    /// <summary>Alumnos del roster con cuota vencida (señal para el profe, no mueve plata).</summary>
    private async Task<HashSet<Guid>> DeudoresDeAsync(IReadOnlyList<Turno> turnos, CancellationToken ct)
    {
        var alumnoIds = turnos
            .SelectMany(t => t.Participantes)
            .Select(p => p.AlumnoId)
            .Distinct()
            .ToList();
        if (alumnoIds.Count == 0) return [];

        var impagos = await _cargos.ListarImpagosAsync(alumnoIds, ct);
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        return impagos
            .GroupBy(c => c.AlumnoId)
            .Where(g => CuotaService.TieneDeudaVencida(g, hoy))
            .Select(g => g.Key)
            .ToHashSet();
    }

    public async Task GenerarTurnosDelMesAsync(int anio, int mes, CancellationToken ct = default)
    {
        var primerDia = new DateOnly(anio, mes, 1);
        var ultimoDia = primerDia.AddMonths(1).AddDays(-1);

        if (await GenerarFaltantesAsync(primerDia, ultimoDia, ct))
            await _turnos.GuardarCambiosAsync(ct);
    }

    /// <summary>
    /// Materializa los turnos que falten en [desde, hasta] desde los horarios
    /// activos. Idempotente: las fechas ya generadas no se tocan (refuerzo
    /// extra: índice único HorarioId+Fecha en la base). NO guarda: el caller
    /// decide cuándo persistir. Devuelve si generó alguno.
    /// </summary>
    private async Task<bool> GenerarFaltantesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var horarios = await _horarios.ListarActivosAsync(ct);
        var bloqueos = await _bloqueos.ListarAsync(ct);
        var generoAlguno = false;

        foreach (var horario in horarios)
        {
            var yaGeneradas = await _turnos.FechasGeneradasAsync(horario.Id, desde, hasta, ct);

            // Roster CONGELADO al generar (fija el divisor del precio):
            // grupal → miembros del grupo que estén ACTIVOS; individual → ese
            // alumno si está activo. El pausado/dado de baja no ocupa lugar ni
            // paga clases a las que no va (y no infla el divisor abaratando al resto).
            List<Guid> roster = [];
            if (horario.GrupoId is not null)
            {
                var grupo = await _grupos.ObtenerAsync(horario.GrupoId.Value, ct);
                if (grupo is not null)
                    roster =
                    [
                        .. grupo.Alumnos
                            .Where(x => x.FechaBaja is null && x.Alumno?.Estado == EstadoAlumno.Activo)
                            .Select(x => x.AlumnoId)
                    ];
            }
            else if (horario.AlumnoId is not null && horario.Alumno?.Estado == EstadoAlumno.Activo)
            {
                roster = [horario.AlumnoId.Value];
            }

            // Sin nadie que juegue no hay turno que generar
            if (roster.Count == 0) continue;

            // Primera fecha del rango que cae en el día del horario, y de ahí de a 7
            var offset = ((int)horario.Dia - (int)desde.DayOfWeek + 7) % 7;
            var alta = DateOnly.FromDateTime(horario.CreadoEl);
            for (var fecha = desde.AddDays(offset); fecha <= hasta; fecha = fecha.AddDays(7))
            {
                if (yaGeneradas.Contains(fecha)) continue;

                // Un horario nuevo no genera (ni cobra) fechas anteriores a
                // su alta: esas clases no existieron
                if (fecha < alta) continue;

                // Slot bloqueado → NO se genera (si el bloqueo se borra,
                // reaparece solo en la próxima generación)
                var f = fecha;
                if (bloqueos.Any(b => BloqueoService.Cubre(
                        b, f, horario.HoraInicio, horario.DuracionMinutos, horario.CanchaId)))
                    continue;

                var turno = new Turno
                {
                    HorarioId = horario.Id,
                    CanchaId = horario.CanchaId,
                    Fecha = fecha,
                    HoraInicio = horario.HoraInicio,
                    DuracionMinutos = horario.DuracionMinutos,
                    // TenantId lo asigna el repositorio
                };
                foreach (var alumnoId in roster)
                    turno.Participantes.Add(new TurnoParticipante { Turno = turno, AlumnoId = alumnoId });

                await _turnos.AgregarAsync(turno, ct);
                generoAlguno = true;
            }
        }

        return generoAlguno;
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

        // Nunca se borra: queda motivo, cuándo y quién (historia)
        turno.Estado = EstadoTurno.Cancelado;
        turno.CanceladoMotivo = motivo;
        turno.CanceladoEl = DateTime.UtcNow;
        turno.CanceladoPor = Models.CanceladoPor.Profesor;

        // La clase no ocurre → nadie paga: cargos impagos fuera, pagados
        // intocables (mismo criterio que la cascada de bloqueos)
        var cargos = await _cargos.ListarPorTurnosAsync([turno.Id], ct);
        foreach (var cargo in cargos.Where(c => c.PagadoEl is null))
            _cargos.Eliminar(cargo);

        await _turnos.GuardarCambiosAsync(ct); // mismo DbContext: persiste todo junto
    }

    private static TurnoResponseDto Mapear(Turno t, HashSet<Guid> deudores) => new()
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
                : t.Participantes.FirstOrDefault()?.Alumno is { } suelto
                    ? $"{suelto.Nombre} {suelto.Apellido} (suelta)" // turno suelto (M5c)
                    : "Clase suelta"),
        Cancha = t.Cancha?.Nombre ?? string.Empty,
        Sede = t.Cancha?.Sede?.Nombre ?? string.Empty,
        Participantes = t.Participantes.Select(p => new ParticipanteTurnoDto
        {
            AlumnoId = p.AlumnoId,
            Nombre = p.Alumno?.Nombre ?? string.Empty,
            Apellido = p.Alumno?.Apellido ?? string.Empty,
            Presente = p.Presente,
            DeudaVencida = deudores.Contains(p.AlumnoId),
        }).ToList(),
    };
}
