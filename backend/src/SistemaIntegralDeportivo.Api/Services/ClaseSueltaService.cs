using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Clase suelta (M5c): el alumno reserva una clase individual en una FECHA
/// puntual. Al pedir se le crea el cargo (precio individual); informa el pago y
/// el profe CONFIRMA (elige cancha, nace el turno suelto, se marca pagado) o
/// RECHAZA (se borra el cargo).
/// </summary>
public interface IClaseSueltaService
{
    Task<ClaseSueltaDto> SolicitarAsync(
        Guid alumnoId, Guid sedeId, DateOnly fecha, TimeOnly hora, int duracionMinutos, CancellationToken ct = default);

    /// <summary>
    /// El PROFE asigna una clase suelta, sin que el alumno la haya pedido: nace ya
    /// CONFIRMADA con su turno. Con <paramref name="generaCargo"/> en false es una
    /// **clase de prueba** y no se le cobra nada.
    /// </summary>
    Task<ClaseSueltaDto> AsignarAsync(
        Guid alumnoId, Guid canchaId, DateOnly fecha, TimeOnly hora, int duracionMinutos,
        Guid? profesorUserId, bool generaCargo, CancellationToken ct = default);

    /// <summary>
    /// Canchas libres en una sede para una FECHA/hora puntual (recurrentes + sueltos de esa
    /// fecha). Con <paramref name="excluirTurnoId"/> el turno que se está editando no
    /// cuenta como ocupante de su propia cancha/horario.
    /// </summary>
    Task<IReadOnlyList<CanchaLibreDto>> CanchasLibresAsync(
        Guid sedeId, DateOnly fecha, TimeOnly hora, int duracionMinutos,
        CancellationToken ct = default, Guid? excluirTurnoId = null);

    /// <summary>Canchas libres para resolver una clase suelta (usa su sede/fecha/hora). Para el profe.</summary>
    Task<IReadOnlyList<CanchaLibreDto>> CanchasLibresParaClaseAsync(Guid claseId, CancellationToken ct = default);

    /// <summary>El alumno avisa que pagó su clase suelta (informa el pago del cargo).</summary>
    Task InformarPagoAsync(Guid alumnoId, Guid claseId, CancellationToken ct = default);

    Task<IReadOnlyList<ClaseSueltaDto>> ListarPendientesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ClaseSueltaDto>> MisAsync(Guid alumnoId, CancellationToken ct = default);
    Task<int> ContarPendientesAsync(CancellationToken ct = default);

    /// <summary>El profe confirma: elige cancha, nace el turno suelto y se marca pagado el cargo.</summary>
    Task ConfirmarAsync(Guid claseId, Guid canchaId, CancellationToken ct = default);

    /// <summary>El profe rechaza: se borra el cargo y la clase queda como historia.</summary>
    Task RechazarAsync(Guid claseId, CancellationToken ct = default);

    /// <summary>
    /// Reprograma una clase suelta ya asignada: solo lo agendable (fecha, hora, cancha,
    /// duración, profe). NO toca al alumno ni el cobro — si ya generó cargo, ese cargo
    /// no se recalcula ni se mueve.
    /// </summary>
    Task EditarAsync(
        Guid turnoId, Guid canchaId, DateOnly fecha, TimeOnly hora, int duracionMinutos,
        Guid? profesorUserId, CancellationToken ct = default);
}

public class ClaseSueltaService : IClaseSueltaService
{
    private readonly IClaseSueltaRepository _clases;
    private readonly IAlumnoRepository _alumnos;
    private readonly ISedeRepository _sedes;
    private readonly IHorarioRepository _horarios;
    private readonly ITurnoRepository _turnos;
    private readonly ICargoRepository _cargos;
    private readonly ITenantRepository _tenant;
    private readonly IStaffService _staff;

    public ClaseSueltaService(
        IClaseSueltaRepository clases, IAlumnoRepository alumnos, ISedeRepository sedes,
        IHorarioRepository horarios, ITurnoRepository turnos, ICargoRepository cargos,
        ITenantRepository tenant, IStaffService staff)
    {
        _clases = clases;
        _alumnos = alumnos;
        _sedes = sedes;
        _horarios = horarios;
        _turnos = turnos;
        _cargos = cargos;
        _tenant = tenant;
        _staff = staff;
    }

    public async Task<ClaseSueltaDto> SolicitarAsync(
        Guid alumnoId, Guid sedeId, DateOnly fecha, TimeOnly hora, int duracionMinutos, CancellationToken ct = default)
    {
        var alumno = await _alumnos.ObtenerAsync(alumnoId, ct)
            ?? throw new ReglaDeNegocioException("El alumno no existe.");
        if (alumno.Estado != EstadoAlumno.Activo)
            throw new ReglaDeNegocioException("Tu cuenta no está activa: hablá con tu profe.");

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        if (fecha < hoy)
            throw new ReglaDeNegocioException("La fecha ya pasó: elegí un día de hoy en adelante.");

        var sede = await _sedes.ObtenerAsync(sedeId, ct);
        if (sede is null || !sede.Activo)
            throw new ReglaDeNegocioException("Esa sede no está disponible.");

        var impagos = await _cargos.ListarImpagosAsync([alumnoId], ct);
        if (CuotaService.TieneDeudaVencida(impagos, hoy))
            throw new ReglaDeNegocioException("Tenés la cuota vencida: regularizala antes de pedir clases nuevas.");

        var tenant = await _tenant.ObtenerActualAsync(ct);
        if (tenant.ValorClaseIndividual is not { } valorHora)
            throw new ReglaDeNegocioException("El profe todavía no configuró el precio de la clase individual.");

        var libres = await CanchasLibresAsync(sedeId, fecha, hora, duracionMinutos, ct);
        if (libres.Count == 0)
            throw new ReglaDeNegocioException($"No hay canchas libres en {sede.Nombre} ese día y hora.");

        // El cargo (precio individual prorrateado por duración) nace impago; el
        // alumno lo informa y el profe lo confirma al habilitar la clase
        var cargo = new Cargo
        {
            AlumnoId = alumnoId,
            Tipo = TipoCargo.Clase,
            Concepto = $"Clase suelta {fecha:dd/MM}",
            Monto = Math.Round(valorHora * duracionMinutos / 60m, 2),
            Fecha = fecha,
        };
        await _cargos.AgregarAsync(cargo, ct);

        var clase = new ClaseSuelta
        {
            AlumnoId = alumnoId,
            SedeId = sedeId,
            Fecha = fecha,
            HoraInicio = hora,
            DuracionMinutos = duracionMinutos,
            CargoId = cargo.Id,
        };
        await _clases.AgregarAsync(clase, ct);
        await _clases.GuardarCambiosAsync(ct);

        clase.Alumno = alumno; clase.Sede = sede; clase.Cargo = cargo; // para el Mapear
        return Mapear(clase);
    }

    public async Task<ClaseSueltaDto> AsignarAsync(
        Guid alumnoId, Guid canchaId, DateOnly fecha, TimeOnly hora, int duracionMinutos,
        Guid? profesorUserId, bool generaCargo, CancellationToken ct = default)
    {
        var alumno = await _alumnos.ObtenerAsync(alumnoId, ct)
            ?? throw new ReglaDeNegocioException("El alumno no existe.");
        if (alumno.Estado != EstadoAlumno.Activo)
            throw new ReglaDeNegocioException(
                $"{alumno.Nombre} no está activo: reactivalo antes de darle una clase.");

        if (fecha < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ReglaDeNegocioException("La fecha ya pasó: elegí un día de hoy en adelante.");

        // NO se valida la deuda vencida: ese guard frena al alumno que se anota solo desde
        // el portal, no al profe armando su agenda (mismo criterio que SelectorAlumnos).

        var sedeId = await _sedes.SedeDeCanchaAsync(canchaId, ct)
            ?? throw new ReglaDeNegocioException("Esa cancha no existe en tu academia.");

        if (profesorUserId is { } profeId)
        {
            if (!await _staff.EsAsignableAsync(profeId, ct))
                throw new ReglaDeNegocioException("Ese profe no está disponible en tu academia.");
            if (!await _staff.TrabajaEnSedeAsync(profeId, sedeId, ct))
                throw new ReglaDeNegocioException("Ese profe no da clases en ese club.");
        }

        var libres = await CanchasLibresAsync(sedeId, fecha, hora, duracionMinutos, ct);
        if (libres.All(c => c.CanchaId != canchaId))
            throw new ReglaDeNegocioException("Esa cancha está ocupada a esa hora. Elegí otra.");

        // Clase de PRUEBA = sin cargo. Si cobra, el cargo nace IMPAGO y se suma a su
        // cuenta corriente junto con la cuota: distinto del camino del alumno, donde se
        // marca pagado al confirmar porque el alumno ya informó la transferencia.
        Cargo? cargo = null;
        if (generaCargo)
        {
            var tenant = await _tenant.ObtenerActualAsync(ct);
            if (tenant.ValorClaseIndividual is not { } valorHora)
                throw new ReglaDeNegocioException("El profe todavía no configuró el precio de la clase individual.");

            cargo = new Cargo
            {
                AlumnoId = alumnoId,
                Tipo = TipoCargo.Clase,
                Concepto = $"Clase suelta {fecha:dd/MM}",
                Monto = Math.Round(valorHora * duracionMinutos / 60m, 2),
                Fecha = fecha,
            };
            await _cargos.AgregarAsync(cargo, ct);
        }

        var turno = new Turno
        {
            HorarioId = null, // suelto: no cuelga de ninguna plantilla
            ProfesorUserId = profesorUserId,
            CanchaId = canchaId,
            Fecha = fecha,
            HoraInicio = hora,
            DuracionMinutos = duracionMinutos,
            Estado = EstadoTurno.Programado,
        };
        turno.Participantes.Add(new TurnoParticipante { Turno = turno, AlumnoId = alumnoId });
        await _turnos.AgregarAsync(turno, ct);

        if (cargo is not null) cargo.TurnoId = turno.Id;

        // Nace CONFIRMADA: la pidió el profe, no hay nada que resolver después.
        var clase = new ClaseSuelta
        {
            AlumnoId = alumnoId,
            SedeId = sedeId,
            Fecha = fecha,
            HoraInicio = hora,
            DuracionMinutos = duracionMinutos,
            Estado = EstadoClaseSuelta.Confirmada,
            CargoId = cargo?.Id,
            CanchaId = canchaId,
            TurnoId = turno.Id,
            ResueltoEl = DateTime.UtcNow,
        };
        await _clases.AgregarAsync(clase, ct);
        await _clases.GuardarCambiosAsync(ct);

        clase.Alumno = alumno; clase.Cargo = cargo; // para el Mapear
        if (await _sedes.ObtenerAsync(sedeId, ct) is { } sede) clase.Sede = sede;
        return Mapear(clase);
    }

    public async Task<IReadOnlyList<CanchaLibreDto>> CanchasLibresAsync(
        Guid sedeId, DateOnly fecha, TimeOnly hora, int duracionMinutos,
        CancellationToken ct = default, Guid? excluirTurnoId = null)
    {
        var sede = await _sedes.ObtenerAsync(sedeId, ct);
        if (sede is null || !sede.Activo) return [];

        var dia = fecha.DayOfWeek;
        var turnosFecha = await _turnos.ListarEntreAsync(fecha, fecha, ct); // turnos ya materializados de esa fecha
        var libres = new List<CanchaLibreDto>();

        foreach (var cancha in sede.Canchas.Where(c => c.Activo))
        {
            // Ocupación recurrente (horarios de ese día de la semana)
            var recurrentes = await _horarios.ListarPorCanchaYDiaAsync(cancha.Id, dia, ct);
            if (recurrentes.Any(h => Solapan(hora, duracionMinutos, h.HoraInicio, h.DuracionMinutos)))
                continue;
            // Ocupación puntual de esa fecha (otras clases sueltas o turnos ya generados, no cancelados).
            // Al editar, el propio turno no cuenta como ocupante de sí mismo.
            if (turnosFecha.Any(t => t.Id != excluirTurnoId && t.CanchaId == cancha.Id
                    && t.Estado == EstadoTurno.Programado
                    && Solapan(hora, duracionMinutos, t.HoraInicio, t.DuracionMinutos)))
                continue;

            libres.Add(new CanchaLibreDto { CanchaId = cancha.Id, Cancha = cancha.Nombre, Sede = sede.Nombre });
        }

        return libres;
    }

    public async Task<IReadOnlyList<CanchaLibreDto>> CanchasLibresParaClaseAsync(
        Guid claseId, CancellationToken ct = default)
    {
        var clase = await _clases.ObtenerAsync(claseId, ct);
        if (clase is null) return [];
        return await CanchasLibresAsync(clase.SedeId, clase.Fecha, clase.HoraInicio, clase.DuracionMinutos, ct);
    }

    public async Task InformarPagoAsync(Guid alumnoId, Guid claseId, CancellationToken ct = default)
    {
        var clase = await _clases.ObtenerAsync(claseId, ct)
            ?? throw new ReglaDeNegocioException("La clase no existe.");
        if (clase.AlumnoId != alumnoId)
            throw new ReglaDeNegocioException("Esa clase no es tuya.");
        if (clase.Estado != EstadoClaseSuelta.Pendiente || clase.Cargo is null)
            throw new ReglaDeNegocioException("Esa clase ya fue resuelta.");
        if (clase.Cargo.PagoInformadoEl is not null)
            throw new ReglaDeNegocioException("Ya avisaste el pago de esa clase.");

        clase.Cargo.PagoInformadoEl = DateTime.UtcNow;
        await _clases.GuardarCambiosAsync(ct);
    }

    public async Task<IReadOnlyList<ClaseSueltaDto>> ListarPendientesAsync(CancellationToken ct = default)
    {
        var pendientes = await _clases.ListarPorEstadoAsync(EstadoClaseSuelta.Pendiente, ct);
        return pendientes.Select(Mapear).ToList();
    }

    public async Task<IReadOnlyList<ClaseSueltaDto>> MisAsync(Guid alumnoId, CancellationToken ct = default)
    {
        var mias = await _clases.ListarPorAlumnoAsync(alumnoId, ct);
        return mias.Select(Mapear).ToList();
    }

    public Task<int> ContarPendientesAsync(CancellationToken ct = default) =>
        _clases.ContarPorEstadoAsync(EstadoClaseSuelta.Pendiente, ct);

    public async Task ConfirmarAsync(Guid claseId, Guid canchaId, CancellationToken ct = default)
    {
        var clase = await _clases.ObtenerAsync(claseId, ct)
            ?? throw new ReglaDeNegocioException("La clase no existe.");
        if (clase.Estado != EstadoClaseSuelta.Pendiente)
            throw new ReglaDeNegocioException("Esa clase ya fue resuelta.");

        // Re-validar que la cancha elegida siga libre a esa fecha/hora
        var libres = await CanchasLibresAsync(clase.SedeId, clase.Fecha, clase.HoraInicio, clase.DuracionMinutos, ct);
        if (libres.All(c => c.CanchaId != canchaId))
            throw new ReglaDeNegocioException("Esa cancha ya no está libre a esa hora. Elegí otra.");

        // Nace el turno SUELTO (sin horario recurrente) con el alumno adentro
        var turno = new Turno
        {
            HorarioId = null,
            CanchaId = canchaId,
            Fecha = clase.Fecha,
            HoraInicio = clase.HoraInicio,
            DuracionMinutos = clase.DuracionMinutos,
            Estado = EstadoTurno.Programado,
        };
        turno.Participantes.Add(new TurnoParticipante { Turno = turno, AlumnoId = clase.AlumnoId });
        await _turnos.AgregarAsync(turno, ct);

        // Se marca pagado el cargo (el alumno informó transferencia) y se linkea al turno
        if (clase.Cargo is not null)
        {
            clase.Cargo.PagadoEl = DateTime.UtcNow;
            clase.Cargo.MedioPago = MedioPago.Transferencia;
            clase.Cargo.TurnoId = turno.Id;
        }

        clase.Estado = EstadoClaseSuelta.Confirmada;
        clase.CanchaId = canchaId;
        clase.TurnoId = turno.Id;
        clase.ResueltoEl = DateTime.UtcNow;
        await _clases.GuardarCambiosAsync(ct);
    }

    public async Task RechazarAsync(Guid claseId, CancellationToken ct = default)
    {
        var clase = await _clases.ObtenerAsync(claseId, ct)
            ?? throw new ReglaDeNegocioException("La clase no existe.");
        if (clase.Estado != EstadoClaseSuelta.Pendiente)
            throw new ReglaDeNegocioException("Esa clase ya fue resuelta.");

        // Rechazada = no hay clase = no se cobra: se borra el cargo (el FK SetNull
        // deja la clase como historia con CargoId en null)
        if (clase.Cargo is not null)
            _cargos.Eliminar(clase.Cargo);

        clase.Estado = EstadoClaseSuelta.Rechazada;
        clase.ResueltoEl = DateTime.UtcNow;
        await _clases.GuardarCambiosAsync(ct);
    }

    public async Task EditarAsync(
        Guid turnoId, Guid canchaId, DateOnly fecha, TimeOnly hora, int duracionMinutos,
        Guid? profesorUserId, CancellationToken ct = default)
    {
        var turno = await _turnos.ObtenerAsync(turnoId, ct)
            ?? throw new ReglaDeNegocioException("El turno no existe.");
        if (turno.HorarioId is not null)
            throw new ReglaDeNegocioException("Esta clase no es suelta.");
        if (turno.Estado == EstadoTurno.Cancelado)
            throw new ReglaDeNegocioException("El turno ya está cancelado.");
        if (fecha < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ReglaDeNegocioException("La fecha ya pasó: elegí un día de hoy en adelante.");

        var sedeId = await _sedes.SedeDeCanchaAsync(canchaId, ct)
            ?? throw new ReglaDeNegocioException("Esa cancha no existe en tu academia.");

        if (profesorUserId is { } profeId)
        {
            if (!await _staff.EsAsignableAsync(profeId, ct))
                throw new ReglaDeNegocioException("Ese profe no está disponible en tu academia.");
            if (!await _staff.TrabajaEnSedeAsync(profeId, sedeId, ct))
                throw new ReglaDeNegocioException("Ese profe no da clases en ese club.");
        }

        // El propio turno no cuenta como ocupante de su horario actual.
        var libres = await CanchasLibresAsync(sedeId, fecha, hora, duracionMinutos, ct, excluirTurnoId: turnoId);
        if (libres.All(c => c.CanchaId != canchaId))
            throw new ReglaDeNegocioException("Esa cancha está ocupada a esa hora. Elegí otra.");

        turno.CanchaId = canchaId;
        turno.Fecha = fecha;
        turno.HoraInicio = hora;
        turno.DuracionMinutos = duracionMinutos;
        turno.ProfesorUserId = profesorUserId;

        // La ClaseSuelta guarda una copia de fecha/hora/cancha para el historial del
        // portal del alumno ("Mis clases"); si no se sincroniza acá queda desactualizada.
        if (await _clases.ObtenerPorTurnoAsync(turnoId, ct) is { } clase)
        {
            clase.SedeId = sedeId;
            clase.CanchaId = canchaId;
            clase.Fecha = fecha;
            clase.HoraInicio = hora;
            clase.DuracionMinutos = duracionMinutos;
        }

        await _turnos.GuardarCambiosAsync(ct); // mismo DbContext: persiste turno y clase juntos
    }

    /// <summary>Dos franjas (inicio+duración) se pisan.</summary>
    private static bool Solapan(TimeOnly iniA, int durA, TimeOnly iniB, int durB) =>
        iniA < iniB.AddMinutes(durB) && iniB < iniA.AddMinutes(durA);

    private static ClaseSueltaDto Mapear(ClaseSuelta c) => new()
    {
        Id = c.Id,
        AlumnoId = c.AlumnoId,
        AlumnoNombre = c.Alumno is null ? string.Empty : $"{c.Alumno.Nombre} {c.Alumno.Apellido}",
        Sede = c.Sede?.Nombre ?? string.Empty,
        Fecha = c.Fecha,
        HoraInicio = c.HoraInicio,
        DuracionMinutos = c.DuracionMinutos,
        Monto = c.Cargo?.Monto ?? 0m,
        Estado = c.Estado.ToString(),
        PagoInformado = c.Cargo is { PagoInformadoEl: not null, PagadoEl: null },
        Pagado = c.Cargo?.PagadoEl is not null,
        Cancha = c.Cancha?.Nombre,
        CreadoEl = c.CreadoEl,
        ResueltoEl = c.ResueltoEl,
    };
}
