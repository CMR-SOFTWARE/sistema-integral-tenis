using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class PortalService : IPortalService
{
    private readonly IAlumnoRepository _alumnos;
    private readonly ITurnoRepository _turnos;
    private readonly ITurnoService _turnoService;
    private readonly ICuotaService _cuotas;
    private readonly ITenantActual _tenantActual;

    public PortalService(
        IAlumnoRepository alumnos, ITurnoRepository turnos,
        ITurnoService turnoService, ICuotaService cuotas,
        ITenantActual tenantActual)
    {
        _alumnos = alumnos;
        _turnos = turnos;
        _turnoService = turnoService;
        _cuotas = cuotas;
        _tenantActual = tenantActual;
    }

    public async Task<MisTurnosDto> MisTurnosAsync(Guid userId, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var proximo = hoy.AddMonths(1);

        // Hacia adelante se materializa lo que falte (generación perezosa
        // idempotente); hacia atrás solo se lista lo que existió
        await _turnoService.GenerarTurnosDelMesAsync(hoy.Year, hoy.Month, ct);
        await _turnoService.GenerarTurnosDelMesAsync(proximo.Year, proximo.Month, ct);

        // Ventana: mes pasado (historial) → fin del mes que viene (próximos)
        var desde = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(-1);
        var hasta = new DateOnly(proximo.Year, proximo.Month, 1).AddMonths(1).AddDays(-1);
        var turnos = await _turnos.ListarPorAlumnoEntreAsync(ficha.Id, desde, hasta, ct);

        return new MisTurnosDto
        {
            Proximos = turnos
                .Where(t => t.Fecha >= hoy)
                .OrderBy(t => t.Fecha).ThenBy(t => t.HoraInicio)
                .Select(t => Mapear(t, ficha.Id))
                .ToList(),
            Historial = turnos
                .Where(t => t.Fecha < hoy)
                .OrderByDescending(t => t.Fecha).ThenBy(t => t.HoraInicio)
                .Select(t => Mapear(t, ficha.Id))
                .ToList(),
        };
    }

    public async Task<AlumnoLiquidacionDto?> MiCuotaAsync(
        Guid userId, int anio, int mes, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);

        // Reusa la liquidación del tenant (genera cargos que falten) y
        // devuelve SOLO la del alumno: el resto no le pertenece
        var liquidacion = await _cuotas.ObtenerMesAsync(anio, mes, ct);
        return liquidacion.Liquidaciones.FirstOrDefault(l => l.AlumnoId == ficha.Id);
    }

    public async Task InformarPagoMesAsync(
        Guid userId, int anio, int mes, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct); // establece el tenant del club
        await _cuotas.InformarPagoMesAsync(ficha.Id, anio, mes, ct);
    }

    public async Task InformarPagoCargoAsync(
        Guid userId, Guid cargoId, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        await _cuotas.InformarPagoCargoAsync(ficha.Id, cargoId, ct);
    }

    public async Task<DatosPagoDto> DatosPagoAsync(Guid userId, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        return new DatosPagoDto
        {
            Club = ficha.Tenant?.Nombre ?? string.Empty,
            AliasCbu = ficha.Tenant?.AliasCbu,
            Titular = ficha.Tenant?.TitularPago,
        };
    }

    public async Task<MiPerfilDto> MiPerfilAsync(Guid userId, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        return new MiPerfilDto
        {
            Nombre = ficha.Nombre,
            Apellido = ficha.Apellido,
            FechaNacimiento = ficha.FechaNacimiento,
            Dni = ficha.Dni,
            Telefono = ficha.Telefono,
            Email = ficha.Email,
            Categoria = ficha.Categoria.ToString(),
            Estado = ficha.Estado.ToString(),
            Modalidad = ficha.Modalidad.ToString(),
            Club = ficha.Tenant?.Nombre ?? string.Empty,
        };
    }

    public async Task<MiPerfilDto> ActualizarMiPerfilAsync(
        Guid userId, ActualizarMiPerfilDto dto, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);

        // El teléfono es el contacto mínimo de la ficha: no puede quedar vacío
        if (string.IsNullOrWhiteSpace(dto.Telefono))
            throw new ReglaDeNegocioException("El teléfono no puede quedar vacío.");

        ficha.Telefono = dto.Telefono.Trim();
        ficha.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        ficha.ActualizadoEl = DateTime.UtcNow;
        await _alumnos.GuardarCambiosAsync(ct);

        return await MiPerfilAsync(userId, ct);
    }

    public async Task CancelarMiTurnoAsync(
        Guid userId, Guid turnoId, string motivo, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);

        if (string.IsNullOrWhiteSpace(motivo))
            throw new ReglaDeNegocioException("Contanos el motivo de la cancelación.");
        if (motivo.Trim().Length > 200)
            throw new ReglaDeNegocioException("El motivo no puede superar los 200 caracteres.");

        var turno = await _turnos.ObtenerAsync(turnoId, ct)
            ?? throw new ReglaDeNegocioException("El turno no existe.");

        // MI participación: si no estoy en el roster, este turno no es mío
        var mia = turno.Participantes.FirstOrDefault(p => p.AlumnoId == ficha.Id)
            ?? throw new ReglaDeNegocioException("No participás de este turno.");

        if (turno.Estado == EstadoTurno.Cancelado)
            throw new ReglaDeNegocioException("La clase ya está cancelada.");
        if (mia.CanceloEl is not null)
            throw new ReglaDeNegocioException("Ya avisaste que no venís a esta clase.");

        // Hasta la hora de inicio (decisión de producto: el aviso no mueve
        // plata, solo informa — sin mínimo de anticipación)
        var ahora = DateTime.UtcNow;
        var hoy = DateOnly.FromDateTime(ahora);
        var yaEmpezo = turno.Fecha < hoy ||
            (turno.Fecha == hoy && turno.HoraInicio <= TimeOnly.FromDateTime(ahora));
        if (yaEmpezo)
            throw new ReglaDeNegocioException("La clase ya empezó o pasó: no se puede cancelar.");

        // Solo MI participación: el turno sigue para el resto y el cargo
        // queda (= falta con aviso, modelo-precios.md; recuperación a
        // discreción del profe)
        mia.CanceloEl = ahora;
        mia.CancelacionMotivo = motivo.Trim();
        mia.Presente = false;
        await _turnos.GuardarCambiosAsync(ct);
    }

    private async Task<Alumno> FichaDeAsync(Guid userId, CancellationToken ct)
    {
        var ficha = await _alumnos.ObtenerPorUserIdAsync(userId, ct)
            ?? throw new ReglaDeNegocioException(
                "Tu cuenta no está vinculada a ningún club todavía. Buscá tu club desde el portal.");

        // COSTURA CLAVE (ADR-0010): el alumno no trae claim tenant — el tenant
        // del request es el del CLUB de su ficha. Con esto, la generación de
        // turnos y la liquidación de cuotas operan el club correcto.
        _tenantActual.Establecer(ficha.TenantId);
        return ficha;
    }

    private static MiTurnoDto Mapear(Turno t, Guid miAlumnoId)
    {
        var mia = t.Participantes.FirstOrDefault(p => p.AlumnoId == miAlumnoId);
        var canceladoPorMi = mia?.CanceloEl is not null;
        var ahora = DateTime.UtcNow;
        var hoy = DateOnly.FromDateTime(ahora);
        var empezo = t.Fecha < hoy ||
            (t.Fecha == hoy && t.HoraInicio <= TimeOnly.FromDateTime(ahora));

        return new MiTurnoDto
        {
            Id = t.Id,
            Fecha = t.Fecha,
            HoraInicio = t.HoraInicio,
            DuracionMinutos = t.DuracionMinutos,
            Titulo = t.Horario?.Grupo?.Nombre ?? "Clase individual",
            Categoria = t.Horario?.Grupo?.Categoria?.ToString(),
            Sede = t.Cancha?.Sede?.Nombre ?? string.Empty,
            Cancha = t.Cancha?.Nombre ?? string.Empty,
            Estado = t.Estado.ToString(),
            CanceladoMotivo = t.CanceladoMotivo,
            Presente = mia?.Presente ?? true,
            Companeros = t.Participantes
                .Where(p => p.AlumnoId != miAlumnoId && p.Alumno is not null)
                .Select(p => $"{p.Alumno!.Nombre} {p.Alumno.Apellido}")
                .ToList(),
            CanceladoPorMi = canceladoPorMi,
            PuedoCancelar = t.Estado == EstadoTurno.Programado && !canceladoPorMi && !empezo,
        };
    }
}
