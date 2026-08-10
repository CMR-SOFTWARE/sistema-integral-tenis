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
    private readonly ISedeRepository _sedes;
    private readonly IUsuarioActual _usuario;
    private readonly IAlumnoService _alumnoService;

    public HorarioService(
        IHorarioRepository horarios, ITurnoRepository turnos, ICargoRepository cargos,
        IBloqueoRepository bloqueos, IStaffService staff, IAlumnoRepository alumnos,
        ISedeRepository sedes, IUsuarioActual usuario, IAlumnoService alumnoService)
    {
        _horarios = horarios;
        _turnos = turnos;
        _cargos = cargos;
        _bloqueos = bloqueos;
        _staff = staff;
        _alumnos = alumnos;
        _sedes = sedes;
        _usuario = usuario;
        _alumnoService = alumnoService;
    }

    public async Task<HorarioResponseDto> CrearAsync(CreateHorarioDto dto, CancellationToken ct = default)
    {
        // El profe EMPLEADO da SUS clases: el horario que crea queda a su nombre (su
        // agenda muestra las clases propias, así que si no, no lo vería). El dueño elige.
        if (_usuario.EsStaff)
            dto.ProfesorUserId = _usuario.UserId;

        // Regla: el cupo no puede nacer más chico que la gente que se está sumando.
        if (dto.CupoMaximo is { } cupo && dto.AlumnoIds.Count > cupo)
            throw new ReglaDeNegocioException(
                $"Elegiste {dto.AlumnoIds.Count} alumnos pero el cupo es de {cupo}.");

        // Regla: el profe EMPLEADO solo arma horarios en canchas de SU club.
        await ValidarCanchaDeSuClubAsync(dto.CanchaId, ct);

        // Regla: la franja tiene que estar libre en esa cancha (sin solapar otro
        // horario ni pisar un bloqueo fijo).
        await ValidarSlotLibreAsync(dto.CanchaId, dto.Dia, dto.HoraInicio, dto.DuracionMinutos, null, ct);

        // Regla: si se asigna un profe, tiene que ser del club (dueño o staff activo)
        if (dto.ProfesorUserId is { } profe && !await _staff.EsAsignableAsync(profe, ct))
            throw new ReglaDeNegocioException("Ese profe no es de tu club.");

        // Los alumnos se validan ANTES de crear nada: si uno no puede, no queda una
        // clase a medio armar.
        var alumnos = new List<Alumno>();
        foreach (var alumnoId in dto.AlumnoIds.Distinct())
            alumnos.Add(await ValidarAlumnoParaClaseAsync(alumnoId, ct));

        var horario = new Horario
        {
            CanchaId = dto.CanchaId,
            Nombre = string.IsNullOrWhiteSpace(dto.Nombre) ? null : dto.Nombre.Trim(),
            CupoMaximo = dto.CupoMaximo,
            Categoria = dto.Categoria,
            ProfesorUserId = dto.ProfesorUserId,
            ValorHoraProfe = dto.ValorHoraProfe,
            Dia = dto.Dia,
            HoraInicio = dto.HoraInicio,
            DuracionMinutos = dto.DuracionMinutos,
            // TenantId lo asigna el repositorio
        };

        var creado = await _horarios.AgregarAsync(horario, ct);

        foreach (var alumno in alumnos)
        {
            // UNA sola instancia por membresía: la misma se registra en el repositorio
            // y se cuelga de la navegación del horario (que ya quedó trackeado al
            // crearse). Con dos objetos distintos —misma PK compuesta— EF los toma
            // como dos filas y tira al guardar.
            var membresia = new AlumnoHorario
            {
                HorarioId = creado.Id,
                AlumnoId = alumno.Id,
                Alumno = alumno, // para armar el título y los miembros de la respuesta
            };
            await _horarios.AgregarMembresiaAsync(membresia, ct);
            creado.Alumnos.Add(membresia);
        }
        await _horarios.GuardarCambiosAsync(ct);

        // No hay nada que "promover": sumarlo al roster ya lo saca de la lista de
        // espera y lo mete en Alumnos, porque las dos listas se derivan de esto mismo.
        return Mapear(creado);
    }

    public async Task AgregarAlumnoAsync(Guid horarioId, Guid alumnoId, CancellationToken ct = default)
    {
        var horario = await _horarios.ObtenerAsync(horarioId, ct)
            ?? throw new ReglaDeNegocioException("La clase no existe.");

        var alumno = await ValidarAlumnoParaClaseAsync(alumnoId, ct);

        // Regla: no duplicar un miembro activo; si tuvo baja previa, se reactiva.
        var membresia = await _horarios.ObtenerMembresiaAsync(horarioId, alumnoId, ct);
        if (membresia is not null && membresia.FechaBaja is null)
            throw new ReglaDeNegocioException(
                $"{alumno.Nombre} {alumno.Apellido} ya está en esta clase.");

        // Regla: respetar el cupo (null = sin límite)
        if (horario.CupoMaximo is not null)
        {
            var activos = await _horarios.ContarActivosAsync(horarioId, ct);
            if (activos >= horario.CupoMaximo)
                throw new ReglaDeNegocioException(
                    $"La clase está completa ({horario.CupoMaximo} lugares).");
        }

        if (membresia is not null)
        {
            // Reactivación: la PK compuesta (AlumnoId, HorarioId) no admite otra fila,
            // así que se reutiliza. Limitación consciente: se pierde el período anterior.
            membresia.FechaAlta = DateTime.UtcNow;
            membresia.FechaBaja = null;
        }
        else
        {
            await _horarios.AgregarMembresiaAsync(
                new AlumnoHorario { HorarioId = horarioId, AlumnoId = alumnoId }, ct);
        }

        // Persistir la membresía ANTES de reconciliar: la reconciliación consulta las
        // membresías activas del alumno (query a la base) y tiene que ver la nueva.
        await _horarios.GuardarCambiosAsync(ct);

        // Lo repone en los turnos futuros YA generados de esta clase y recalcula el
        // divisor de la cuota: sin esto, el que vuelve no aparece en el calendario.
        await _alumnoService.SincronizarCalendarioAsync(alumnoId, ct);
        await _horarios.GuardarCambiosAsync(ct);
    }

    public async Task QuitarAlumnoAsync(Guid horarioId, Guid alumnoId, CancellationToken ct = default)
    {
        var membresia = await _horarios.ObtenerMembresiaAsync(horarioId, alumnoId, ct);
        if (membresia is null || membresia.FechaBaja is not null)
            throw new ReglaDeNegocioException("Ese alumno no está en esta clase.");

        // Baja lógica: se conserva la historia ("estuvo de marzo a junio")
        membresia.FechaBaja = DateTime.UtcNow;
        await _horarios.GuardarCambiosAsync(ct);

        // Ya no viene a esta clase: sacarlo de sus turnos futuros (sigue Activo y en
        // sus OTRAS clases, así que la reconciliación solo lo saca de esta).
        await _alumnoService.SincronizarCalendarioAsync(alumnoId, ct);
        await _horarios.GuardarCambiosAsync(ct);
    }

    /// <summary>
    /// Quién puede entrar a una clase cuando lo pone EL PROFE: los ACTIVOS. Eso incluye
    /// a los de la lista de espera, que son activos sin clase todavía — sumarlos es
    /// justamente lo que los vuelve alumnos. Quedan afuera el pausado y el de baja.
    ///
    /// La deuda vencida NO frena acá: el profe decide a quién le da clase, y muchas
    /// veces le arma el horario justamente para acomodarlo. La regla sigue viva del
    /// lado del ALUMNO, que no puede pedir clases nuevas debiendo — en
    /// <see cref="SolicitudCupoService"/>, <see cref="SolicitudHorarioService"/> y
    /// <see cref="ClaseSueltaService"/>.
    /// </summary>
    private async Task<Alumno> ValidarAlumnoParaClaseAsync(Guid alumnoId, CancellationToken ct)
    {
        var alumno = await _alumnos.ObtenerAsync(alumnoId, ct)
            ?? throw new ReglaDeNegocioException("El alumno no existe.");

        if (alumno.Estado != EstadoAlumno.Activo)
            throw new ReglaDeNegocioException(
                $"{alumno.Nombre} {alumno.Apellido} no está activo y no puede sumarse a una clase.");

        return alumno;
    }

    public async Task<HorarioResponseDto> AsignarProfesorAsync(
        Guid id, Guid? profesorUserId, decimal? valorHoraProfe, CancellationToken ct = default)
    {
        var horario = await _horarios.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("El horario no existe.");

        await ValidarCanchaDeSuClubAsync(horario.CanchaId, ct);

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

        // El staff solo toca horarios de su club: la cancha ACTUAL y la DESTINO
        // deben ser de su sede (no puede sacar una clase de otro club ni meterla en él).
        await ValidarCanchaDeSuClubAsync(horario.CanchaId, ct);
        await ValidarCanchaDeSuClubAsync(dto.CanchaId, ct);

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

        // El cupo no puede quedar por debajo de la gente que ya viene.
        if (dto.CupoMaximo is { } cupo)
        {
            var activos = await _horarios.ContarActivosAsync(id, ct);
            if (activos > cupo)
                throw new ReglaDeNegocioException(
                    $"Ya hay {activos} alumnos en esta clase: el cupo no puede ser menor.");
        }

        horario.CanchaId = dto.CanchaId;
        horario.Nombre = string.IsNullOrWhiteSpace(dto.Nombre) ? null : dto.Nombre.Trim();
        horario.CupoMaximo = dto.CupoMaximo;
        horario.Categoria = dto.Categoria;
        horario.ProfesorUserId = dto.ProfesorUserId;
        horario.ValorHoraProfe = dto.ProfesorUserId is null ? null : dto.ValorHoraProfe;
        horario.Dia = dto.Dia;
        horario.HoraInicio = dto.HoraInicio;
        horario.DuracionMinutos = dto.DuracionMinutos;

        await _horarios.GuardarCambiosAsync(ct);
        return Mapear(await _horarios.ObtenerConRosterAsync(id, ct) ?? horario);
    }

    public async Task<IReadOnlyList<HorarioResponseDto>> ListarAsync(CancellationToken ct = default)
    {
        IEnumerable<Horario> horarios = await _horarios.ListarActivosAsync(ct);

        // El profe EMPLEADO ve solo los horarios de SU club; el dueño, todos.
        if (_usuario.EsStaff)
        {
            var sede = await _staff.SedeDelProfeAsync(_usuario.UserId!.Value, ct);
            horarios = horarios.Where(h => h.Cancha?.SedeId == sede);
        }

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
    /// El profe EMPLEADO solo gestiona horarios en canchas de SU club (sede). El
    /// dueño trabaja en cualquiera. Si el staff no tiene sede, o la cancha es de
    /// otra, se rechaza. (La granularidad de "no pisar" ya la da la cancha.)
    /// </summary>
    private async Task ValidarCanchaDeSuClubAsync(Guid canchaId, CancellationToken ct)
    {
        if (!_usuario.EsStaff) return;

        var sedeStaff = await _staff.SedeDelProfeAsync(_usuario.UserId!.Value, ct);
        var sedeCancha = await _sedes.SedeDeCanchaAsync(canchaId, ct);
        if (sedeStaff is null || sedeCancha != sedeStaff)
            throw new ReglaDeNegocioException("Esa cancha no es de tu club.");
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

    /// <summary>
    /// Cómo se llama la clase cuando el profe no le puso nombre: si va uno solo, es
    /// su nombre (era la "clase individual"); si van varios, cuántos son.
    /// </summary>
    public static string TituloDe(string? nombre, IEnumerable<AlumnoHorario> roster)
    {
        if (!string.IsNullOrWhiteSpace(nombre)) return nombre;

        var activos = roster.Where(x => x.FechaBaja is null).ToList();
        if (activos.Count == 1 && activos[0].Alumno is { } unico)
            return $"{unico.Nombre} {unico.Apellido}";

        return activos.Count == 0 ? "Clase sin alumnos" : $"Grupo de {activos.Count}";
    }

    private static HorarioResponseDto Mapear(Horario h)
    {
        var activos = h.Alumnos.Where(x => x.FechaBaja is null).ToList();
        return new HorarioResponseDto
        {
            Id = h.Id,
            Titulo = TituloDe(h.Nombre, h.Alumnos),
            Nombre = h.Nombre,
            Categoria = h.Categoria?.ToString(),
            CupoMaximo = h.CupoMaximo,
            CanchaId = h.CanchaId,
            Cancha = h.Cancha?.Nombre ?? string.Empty,
            Sede = h.Cancha?.Sede?.Nombre ?? string.Empty,
            Dia = h.Dia,
            HoraInicio = h.HoraInicio,
            DuracionMinutos = h.DuracionMinutos,
            Activo = h.Activo,
            ProfesorUserId = h.ProfesorUserId,
            ValorHoraProfe = h.ValorHoraProfe,
            MiembrosActivos = activos.Count,
            Miembros = [.. activos
                .Where(x => x.Alumno is not null)
                .OrderBy(x => x.Alumno!.Apellido).ThenBy(x => x.Alumno!.Nombre)
                .Select(x => new MiembroHorarioDto
                {
                    AlumnoId = x.AlumnoId,
                    Nombre = x.Alumno!.Nombre,
                    Apellido = x.Alumno.Apellido,
                    Categoria = x.Alumno.Categoria.ToString(),
                    FechaAlta = x.FechaAlta,
                })],
        };
    }
}
