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

    /// <summary>Canchas libres en una sede para una FECHA/hora puntual (recurrentes + sueltos de esa fecha).</summary>
    Task<IReadOnlyList<CanchaLibreDto>> CanchasLibresAsync(
        Guid sedeId, DateOnly fecha, TimeOnly hora, int duracionMinutos, CancellationToken ct = default);

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

    public ClaseSueltaService(
        IClaseSueltaRepository clases, IAlumnoRepository alumnos, ISedeRepository sedes,
        IHorarioRepository horarios, ITurnoRepository turnos, ICargoRepository cargos, ITenantRepository tenant)
    {
        _clases = clases;
        _alumnos = alumnos;
        _sedes = sedes;
        _horarios = horarios;
        _turnos = turnos;
        _cargos = cargos;
        _tenant = tenant;
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

    public async Task<IReadOnlyList<CanchaLibreDto>> CanchasLibresAsync(
        Guid sedeId, DateOnly fecha, TimeOnly hora, int duracionMinutos, CancellationToken ct = default)
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
            // Ocupación puntual de esa fecha (otras clases sueltas o turnos ya generados, no cancelados)
            if (turnosFecha.Any(t => t.CanchaId == cancha.Id && t.Estado == EstadoTurno.Programado
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
