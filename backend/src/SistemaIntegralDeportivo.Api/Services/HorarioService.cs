using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class HorarioService : IHorarioService
{
    private readonly IHorarioRepository _horarios;
    private readonly ITurnoRepository _turnos;
    private readonly ICargoRepository _cargos;
    private readonly IBloqueoRepository _bloqueos;
    private readonly IStaffService _staff;
    private readonly IAlumnoRepository _alumnos;

    public HorarioService(
        IHorarioRepository horarios, ITurnoRepository turnos, ICargoRepository cargos,
        IBloqueoRepository bloqueos, IStaffService staff, IAlumnoRepository alumnos)
    {
        _horarios = horarios;
        _turnos = turnos;
        _cargos = cargos;
        _bloqueos = bloqueos;
        _staff = staff;
        _alumnos = alumnos;
    }

    public async Task<HorarioResponseDto> CrearAsync(CreateHorarioDto dto, CancellationToken ct = default)
    {
        // Regla: grupal XOR individual — exactamente uno de los dos
        var tieneGrupo = dto.GrupoId is not null;
        var tieneAlumno = dto.AlumnoId is not null;
        if (tieneGrupo == tieneAlumno) // ambos o ninguno
            throw new ReglaDeNegocioException(
                "El horario debe apuntar a un grupo O a un alumno individual (exactamente uno).");

        // Regla: nadie toma clases NUEVAS con la cuota vencida (las ya asignadas siguen)
        if (dto.AlumnoId is not null)
        {
            var impagos = await _cargos.ListarImpagosAsync([dto.AlumnoId.Value], ct);
            if (CuotaService.TieneDeudaVencida(impagos, DateOnly.FromDateTime(DateTime.UtcNow)))
                throw new ReglaDeNegocioException(
                    "El alumno tiene la cuota vencida: registrá el pago en Cuotas antes de asignarle clases nuevas.");
        }

        // Regla: la franja tiene que estar libre en esa cancha (sin solapar otro
        // horario ni pisar un bloqueo fijo).
        await ValidarSlotLibreAsync(dto.CanchaId, dto.Dia, dto.HoraInicio, dto.DuracionMinutos, null, ct);

        // Regla: si se asigna un profe, tiene que ser del club (dueño o staff activo)
        if (dto.ProfesorUserId is { } profe && !await _staff.EsAsignableAsync(profe, ct))
            throw new ReglaDeNegocioException("Ese profe no es de tu club.");

        var horario = new Horario
        {
            CanchaId = dto.CanchaId,
            GrupoId = dto.GrupoId,
            AlumnoId = dto.AlumnoId,
            ProfesorUserId = dto.ProfesorUserId,
            ValorHoraProfe = dto.ValorHoraProfe,
            Dia = dto.Dia,
            HoraInicio = dto.HoraInicio,
            DuracionMinutos = dto.DuracionMinutos,
            // TenantId lo asigna el repositorio
        };

        var creado = await _horarios.AgregarAsync(horario, ct);

        // Un horario individual es "darle su primera clase": si el alumno estaba en
        // la lista de espera, ahora es alumno de verdad (Activo).
        if (dto.AlumnoId is { } alumnoId)
            await _alumnos.PromoverDeEsperaAsync(alumnoId, ct);

        return Mapear(creado);
    }

    public async Task<HorarioResponseDto> AsignarProfesorAsync(
        Guid id, Guid? profesorUserId, decimal? valorHoraProfe, CancellationToken ct = default)
    {
        var horario = await _horarios.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("El horario no existe.");

        if (profesorUserId is { } profe && !await _staff.EsAsignableAsync(profe, ct))
            throw new ReglaDeNegocioException("Ese profe no es de tu club.");

        horario.ProfesorUserId = profesorUserId;
        // El valor hora acompaña al profe: null = usar el base del profe (o queda
        // sin profe → sin valor). Sacar al profe limpia el override.
        horario.ValorHoraProfe = profesorUserId is null ? null : valorHoraProfe;
        await _horarios.GuardarCambiosAsync(ct);
        return Mapear(horario);
    }

    public async Task<HorarioResponseDto> EditarAsync(
        Guid id, UpdateHorarioDto dto, CancellationToken ct = default)
    {
        var horario = await _horarios.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("El horario no existe.");

        // La franja destino tiene que estar libre (excluyéndose a sí mismo, así
        // editar sin moverlo no se marca como "se pisa con otro").
        await ValidarSlotLibreAsync(dto.CanchaId, dto.Dia, dto.HoraInicio, dto.DuracionMinutos, id, ct);

        if (dto.ProfesorUserId is { } profe && !await _staff.EsAsignableAsync(profe, ct))
            throw new ReglaDeNegocioException("Ese profe no es de tu club.");

        // Si cambió el horario EN SÍ (cancha/día/hora/duración), los turnos futuros ya
        // no corresponden: se limpian (los pagados no) y la generación perezosa los
        // rehace con el nuevo horario. Cambiar solo el profe/valor no toca los turnos.
        var cambioAgenda = horario.CanchaId != dto.CanchaId
            || horario.Dia != dto.Dia
            || horario.HoraInicio != dto.HoraInicio
            || horario.DuracionMinutos != dto.DuracionMinutos;
        if (cambioAgenda)
            await LimpiarTurnosFuturosAsync(id, ct);

        horario.CanchaId = dto.CanchaId;
        horario.ProfesorUserId = dto.ProfesorUserId;
        horario.ValorHoraProfe = dto.ProfesorUserId is null ? null : dto.ValorHoraProfe;
        horario.Dia = dto.Dia;
        horario.HoraInicio = dto.HoraInicio;
        horario.DuracionMinutos = dto.DuracionMinutos;

        await _horarios.GuardarCambiosAsync(ct);
        return Mapear(horario);
    }

    public async Task<IReadOnlyList<HorarioResponseDto>> ListarAsync(CancellationToken ct = default)
    {
        var horarios = await _horarios.ListarActivosAsync(ct);
        return horarios.Select(Mapear).ToList();
    }

    public async Task DesactivarAsync(Guid id, CancellationToken ct = default)
    {
        var horario = await _horarios.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("El horario no existe.");

        horario.Activo = false;
        await LimpiarTurnosFuturosAsync(id, ct);
        await _horarios.GuardarCambiosAsync(ct); // mismo DbContext: persiste todo junto
    }

    /// <summary>
    /// La franja (cancha + día + rango horario) tiene que estar libre: sin solapar
    /// otro horario de esa cancha ni pisar un BLOQUEO FIJO. Al editar, <paramref
    /// name="excluirId"/> es el propio horario (no se pisa consigo mismo). Los
    /// bloqueos por RANGO (fecha puntual) no frenan el recurrente: solo saltean el
    /// turno de esa fecha (lo maneja la generación).
    /// </summary>
    private async Task ValidarSlotLibreAsync(
        Guid canchaId, DayOfWeek dia, TimeOnly horaInicio, int duracionMinutos,
        Guid? excluirId, CancellationToken ct)
    {
        var fin = horaInicio.AddMinutes(duracionMinutos);

        var delDia = await _horarios.ListarPorCanchaYDiaAsync(canchaId, dia, ct);
        var pisado = delDia.FirstOrDefault(h =>
            h.Id != excluirId
            && horaInicio < h.HoraInicio.AddMinutes(h.DuracionMinutos) && h.HoraInicio < fin);
        if (pisado is not null)
            throw new ReglaDeNegocioException(
                $"Se superpone con otro horario de esa cancha ({pisado.HoraInicio:HH\\:mm}, {pisado.DuracionMinutos}').");

        var bloqueos = await _bloqueos.ListarAsync(ct);
        var bloqueado = bloqueos.FirstOrDefault(b =>
            b.Tipo == TipoBloqueo.Fijo
            && b.Dia == dia
            && (b.CanchaId is null || b.CanchaId == canchaId)
            && horaInicio < b.HoraFin && b.HoraInicio < fin);
        if (bloqueado is not null)
            throw new ReglaDeNegocioException(
                $"Ese día y horario están bloqueados ({bloqueado.HoraInicio:HH\\:mm}–{bloqueado.HoraFin:HH\\:mm}). " +
                "Sacá el bloqueo o elegí otro horario.");
    }

    /// <summary>
    /// Borra los turnos FUTUROS (≥ hoy) del horario y sus cargos impagos, para no
    /// dejar facturado algo que ya no va a ocurrir (baja o cambio de horario). Los
    /// turnos con algún cargo PAGADO se conservan: la plata cobrada no se rompe. NO
    /// guarda: el caller persiste todo junto.
    /// </summary>
    private async Task LimpiarTurnosFuturosAsync(Guid horarioId, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var futuros = await _turnos.ListarPorHorarioDesdeAsync(horarioId, hoy, ct);
        if (futuros.Count == 0) return;

        var cargos = await _cargos.ListarPorTurnosAsync(futuros.Select(t => t.Id).ToList(), ct);
        var porTurno = cargos.ToLookup(c => c.TurnoId!.Value);
        foreach (var turno in futuros)
        {
            if (porTurno[turno.Id].Any(c => c.PagadoEl is not null)) continue;

            foreach (var cargo in porTurno[turno.Id])
                _cargos.Eliminar(cargo);
            _turnos.Eliminar(turno);
        }
    }

    private static HorarioResponseDto Mapear(Horario h) => new()
    {
        Id = h.Id,
        Titulo = h.Grupo?.Nombre
            ?? (h.Alumno is not null ? $"{h.Alumno.Nombre} {h.Alumno.Apellido} (individual)" : string.Empty),
        Categoria = h.Grupo?.Categoria?.ToString() ?? h.Alumno?.Categoria.ToString(),
        EsIndividual = h.AlumnoId is not null,
        CanchaId = h.CanchaId,
        Cancha = h.Cancha?.Nombre ?? string.Empty,
        Sede = h.Cancha?.Sede?.Nombre ?? string.Empty,
        Dia = h.Dia,
        HoraInicio = h.HoraInicio,
        DuracionMinutos = h.DuracionMinutos,
        Activo = h.Activo,
        ProfesorUserId = h.ProfesorUserId,
        ValorHoraProfe = h.ValorHoraProfe,
        GrupoId = h.GrupoId,
        AlumnoId = h.AlumnoId,
    };
}
