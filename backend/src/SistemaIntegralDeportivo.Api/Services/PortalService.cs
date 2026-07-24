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
    private readonly IServicioService _servicios;
    private readonly IPedidoService _pedidos;
    private readonly IRaquetaService _raquetas;
    private readonly ISolicitudGrupoService _solicitudesGrupo;
    private readonly ISolicitudHorarioService _solicitudesHorario;
    private readonly IClaseSueltaService _clasesSueltas;
    private readonly IPublicidadService _publicidad;
    private readonly IAvisoService _avisos;
    private readonly INotaAlumnoService _notas;
    private readonly ISedeRepository _sedes;
    private readonly ITenantActual _tenantActual;
    private readonly IFichaActual _fichaActual;

    public PortalService(
        IAlumnoRepository alumnos, ITurnoRepository turnos,
        ITurnoService turnoService, ICuotaService cuotas,
        IServicioService servicios, IPedidoService pedidos,
        IRaquetaService raquetas, ISolicitudGrupoService solicitudesGrupo,
        ISolicitudHorarioService solicitudesHorario, IClaseSueltaService clasesSueltas,
        IPublicidadService publicidad, IAvisoService avisos, INotaAlumnoService notas,
        ISedeRepository sedes, ITenantActual tenantActual, IFichaActual fichaActual)
    {
        _alumnos = alumnos;
        _turnos = turnos;
        _turnoService = turnoService;
        _cuotas = cuotas;
        _servicios = servicios;
        _pedidos = pedidos;
        _raquetas = raquetas;
        _solicitudesGrupo = solicitudesGrupo;
        _solicitudesHorario = solicitudesHorario;
        _clasesSueltas = clasesSueltas;
        _publicidad = publicidad;
        _avisos = avisos;
        _notas = notas;
        _sedes = sedes;
        _tenantActual = tenantActual;
        _fichaActual = fichaActual;
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

    public async Task<IReadOnlyList<ServicioDto>> ServiciosAsync(Guid userId, CancellationToken ct = default)
    {
        await FichaDeAsync(userId, ct); // establece el tenant del club
        return await _servicios.ListarAsync(soloActivos: true, ct);
    }

    public async Task<PedidoDto> PedirServicioAsync(
        Guid userId, Guid servicioId, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        return await _pedidos.PedirAsync(ficha.Id, servicioId, ct);
    }

    public async Task<IReadOnlyList<PedidoDto>> MisPedidosAsync(Guid userId, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        return await _pedidos.MisPedidosAsync(ficha.Id, ct);
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
            FotoUrl = ficha.FotoUrl,
            Raquetas = [.. await _raquetas.MisAsync(ficha.Id, ct)],
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
        // Categoría editable por el alumno "por ahora": es un solo campo en la
        // ficha, así que el cambio se refleja en todos lados (lista del profe,
        // ficha, cuotas). Todavía NO se valida contra sus grupos (ver M5).
        ficha.Categoria = dto.Categoria;
        ficha.ActualizadoEl = DateTime.UtcNow;
        await _alumnos.GuardarCambiosAsync(ct);

        return await MiPerfilAsync(userId, ct);
    }

    public async Task<MiPerfilDto> ActualizarFotoAsync(
        Guid userId, string? fotoUrl, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);

        if (string.IsNullOrWhiteSpace(fotoUrl))
        {
            ficha.FotoUrl = null; // quitar la foto
        }
        else
        {
            // Guardamos la imagen comprimida como data URL (sin storage externo).
            // Validación mínima: que sea imagen y no exceda un tamaño razonable.
            if (!fotoUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                throw new ReglaDeNegocioException("La foto tiene que ser una imagen.");
            if (fotoUrl.Length > 700_000) // ~500 KB de imagen en base64
                throw new ReglaDeNegocioException("La foto es muy pesada: probá con una más chica.");
            ficha.FotoUrl = fotoUrl;
        }

        ficha.ActualizadoEl = DateTime.UtcNow;
        await _alumnos.GuardarCambiosAsync(ct);
        return await MiPerfilAsync(userId, ct);
    }

    // ── Reservar horario fijo grupal (M5a) ──

    public async Task<IReadOnlyList<GrupoDisponibleDto>> GruposDisponiblesAsync(
        Guid userId, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        return await _solicitudesGrupo.DisponiblesParaAlumnoAsync(ficha.Id, ct);
    }

    public async Task<SolicitudGrupoDto> SolicitarGrupoAsync(
        Guid userId, Guid grupoId, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        return await _solicitudesGrupo.SolicitarAsync(ficha.Id, grupoId, ct);
    }

    public async Task<IReadOnlyList<SolicitudGrupoDto>> MisSolicitudesGrupoAsync(
        Guid userId, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        return await _solicitudesGrupo.MisAsync(ficha.Id, ct);
    }

    // ── Clase individual fija (M5b) ──

    public async Task<SolicitudHorarioDto> SolicitarHorarioAsync(
        Guid userId, Guid sedeId, DayOfWeek dia, TimeOnly hora, int duracionMinutos, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        return await _solicitudesHorario.SolicitarAsync(ficha.Id, sedeId, dia, hora, duracionMinutos, ct);
    }

    // ── Clase suelta (M5c) ──

    public async Task<ClaseSueltaDto> SolicitarClaseSueltaAsync(
        Guid userId, Guid sedeId, DateOnly fecha, TimeOnly hora, int duracionMinutos, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        return await _clasesSueltas.SolicitarAsync(ficha.Id, sedeId, fecha, hora, duracionMinutos, ct);
    }

    public async Task<IReadOnlyList<ClaseSueltaDto>> MisClasesSueltasAsync(Guid userId, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        return await _clasesSueltas.MisAsync(ficha.Id, ct);
    }

    public async Task InformarPagoClaseSueltaAsync(Guid userId, Guid claseId, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        await _clasesSueltas.InformarPagoAsync(ficha.Id, claseId, ct);
    }

    public async Task<DisponibilidadDto> DisponibilidadClaseSueltaAsync(
        Guid userId, Guid sedeId, DateOnly fecha, TimeOnly hora, int duracionMinutos, CancellationToken ct = default)
    {
        await FichaDeAsync(userId, ct);
        var libres = await _clasesSueltas.CanchasLibresAsync(sedeId, fecha, hora, duracionMinutos, ct);
        return new DisponibilidadDto { HayLugar = libres.Count > 0, CanchasLibres = libres.Count };
    }

    public async Task<IReadOnlyList<PublicidadDto>> PublicidadAsync(Guid userId, CancellationToken ct = default)
    {
        await FichaDeAsync(userId, ct); // establece el tenant del club → sus banners
        return await _publicidad.ListarAsync(soloActivas: true, ct);
    }

    public async Task<IReadOnlyList<AvisoDto>> AvisosAsync(Guid userId, CancellationToken ct = default)
    {
        await FichaDeAsync(userId, ct); // establece el tenant del club → sus avisos
        return await _avisos.ListarAsync(soloVigentes: true, ct); // solo activos y no vencidos
    }

    public async Task<IReadOnlyList<NotaAlumnoDto>> NotasAsync(Guid userId, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct); // establece el tenant y me da mi ficha
        return await _notas.ListarAsync(ficha.Id, soloCompartidas: true, ct); // solo lo que el profe compartió
    }

    public async Task<IReadOnlyList<SedeReservaDto>> SedesAsync(Guid userId, CancellationToken ct = default)
    {
        await FichaDeAsync(userId, ct); // establece el tenant del club
        var sedes = await _sedes.ListarAsync(ct);
        return sedes
            .Where(x => x.Activo)
            .Select(x => new SedeReservaDto { Id = x.Id, Nombre = x.Nombre })
            .ToList();
    }

    public async Task<IReadOnlyList<SolicitudHorarioDto>> MisSolicitudesHorarioAsync(
        Guid userId, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        return await _solicitudesHorario.MisAsync(ficha.Id, ct);
    }

    public async Task<DisponibilidadDto> DisponibilidadHorarioAsync(
        Guid userId, Guid sedeId, DayOfWeek dia, TimeOnly hora, int duracionMinutos, CancellationToken ct = default)
    {
        await FichaDeAsync(userId, ct); // establece el tenant del club
        var libres = await _solicitudesHorario.CanchasLibresAsync(sedeId, dia, hora, duracionMinutos, ct);
        return new DisponibilidadDto { HayLugar = libres.Count > 0, CanchasLibres = libres.Count };
    }

    public async Task<IReadOnlyList<RaquetaDto>> MisRaquetasAsync(Guid userId, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        return await _raquetas.MisAsync(ficha.Id, ct);
    }

    public async Task<RaquetaDto> AgregarRaquetaAsync(
        Guid userId, GuardarRaquetaDto dto, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        return await _raquetas.AgregarAsync(ficha.Id, dto, ct);
    }

    public async Task<RaquetaDto> EditarRaquetaAsync(
        Guid userId, Guid raquetaId, GuardarRaquetaDto dto, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        return await _raquetas.EditarAsync(ficha.Id, raquetaId, dto, ct);
    }

    public async Task BorrarRaquetaAsync(Guid userId, Guid raquetaId, CancellationToken ct = default)
    {
        var ficha = await FichaDeAsync(userId, ct);
        await _raquetas.BorrarAsync(ficha.Id, raquetaId, ct);
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
        Alumno? ficha;
        if (_fichaActual.AlumnoId is { } alumnoId)
        {
            // Ficha elegida en el selector familiar: tiene que ser de la cuenta del titular
            var fichas = await _alumnos.ListarPorUserIdAsync(userId, ct);
            ficha = fichas.FirstOrDefault(f => f.Id == alumnoId)
                ?? throw new ReglaDeNegocioException("Esa ficha no es de tu cuenta.");
        }
        else
        {
            ficha = await _alumnos.ObtenerPorUserIdAsync(userId, ct)
                ?? throw new ReglaDeNegocioException(
                    "Tu cuenta no está vinculada a ningún club todavía. Buscá tu club desde el portal.");
        }

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
