using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class BloqueoService : IBloqueoService
{
    private readonly IBloqueoRepository _bloqueos;
    private readonly ITurnoRepository _turnos;
    private readonly ICargoRepository _cargos;

    public BloqueoService(IBloqueoRepository bloqueos, ITurnoRepository turnos, ICargoRepository cargos)
    {
        _bloqueos = bloqueos;
        _turnos = turnos;
        _cargos = cargos;
    }

    public async Task<IReadOnlyList<BloqueoResponseDto>> ListarAsync(CancellationToken ct = default) =>
        (await _bloqueos.ListarAsync(ct)).Select(Mapear).ToList();

    public async Task<ImpactoBloqueoDto> PrevisualizarImpactoAsync(
        CreateBloqueoDto dto, CancellationToken ct = default)
    {
        Validar(dto);
        var pisados = await TurnosPisadosAsync(dto, ct);
        return ArmarImpacto(pisados);
    }

    public async Task<BloqueoCreadoDto> CrearAsync(CreateBloqueoDto dto, CancellationToken ct = default)
    {
        Validar(dto);

        // ── Cascada: cancelar los turnos programados que el bloqueo pisa.
        //    Nadie paga (cancelación del profe): cargos impagos fuera,
        //    pagados intocables — mismo criterio que HorarioService ──
        var pisados = await TurnosPisadosAsync(dto, ct);
        var motivo = dto.Tipo == TipoBloqueo.Fijo ? "Bloqueo fijo" : $"Bloqueo: {MotivoLegible(dto.Motivo!.Value)}";
        var ahora = DateTime.UtcNow;

        if (pisados.Count > 0)
        {
            var cargos = await _cargos.ListarPorTurnosAsync(pisados.Select(t => t.Id).ToList(), ct);
            foreach (var cargo in cargos.Where(c => c.PagadoEl is null))
                _cargos.Eliminar(cargo);

            foreach (var turno in pisados)
            {
                turno.Estado = EstadoTurno.Cancelado;
                turno.CanceladoMotivo = motivo;
                turno.CanceladoEl = ahora;
                turno.CanceladoPor = CanceladoPor.Profesor; // el bloqueo es del profe
            }
        }

        var bloqueo = new Bloqueo
        {
            Tipo = dto.Tipo,
            Dia = dto.Tipo == TipoBloqueo.Fijo ? dto.Dia : null,
            Fecha = dto.Tipo == TipoBloqueo.Rango ? dto.Fecha : null,
            HoraInicio = dto.HoraInicio,
            HoraFin = dto.HoraFin,
            CanchaId = dto.CanchaId,
            Motivo = dto.Tipo == TipoBloqueo.Rango ? dto.Motivo : null,
            // TenantId lo asigna el repositorio
        };
        await _bloqueos.AgregarAsync(bloqueo, ct);

        // Mismo DbContext: bloqueo + cancelaciones + cargos en una transacción
        await _bloqueos.GuardarCambiosAsync(ct);

        return new BloqueoCreadoDto { Bloqueo = Mapear(bloqueo), Impacto = ArmarImpacto(pisados) };
    }

    public async Task EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var bloqueo = await _bloqueos.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("El bloqueo no existe.");

        // Los turnos que la cascada canceló NO se restauran (historia); los
        // slots futuros no generados reaparecen solos en la próxima generación
        _bloqueos.Eliminar(bloqueo);
        await _bloqueos.GuardarCambiosAsync(ct);
    }

    /// <summary>
    /// ¿El bloqueo cubre este slot? Compartido por la cascada, el preview y
    /// el salteo de la generación perezosa (TurnoService).
    /// </summary>
    public static bool Cubre(Bloqueo b, DateOnly fecha, TimeOnly horaInicio, int duracionMinutos, Guid canchaId)
    {
        if (b.CanchaId is not null && b.CanchaId != canchaId) return false;

        var coincideDia = b.Tipo == TipoBloqueo.Fijo
            ? b.Dia == fecha.DayOfWeek
            : b.Fecha == fecha;
        if (!coincideDia) return false;

        // Solape de franjas [inicio, fin): tocar el borde no cuenta
        var horaFin = horaInicio.AddMinutes(duracionMinutos);
        return horaInicio < b.HoraFin && b.HoraInicio < horaFin;
    }

    private static void Validar(CreateBloqueoDto dto)
    {
        if (dto.HoraFin <= dto.HoraInicio)
            throw new ReglaDeNegocioException("La hora de fin debe ser posterior a la de inicio.");

        if (dto.Tipo == TipoBloqueo.Fijo)
        {
            if (dto.Dia is null)
                throw new ReglaDeNegocioException("Un bloqueo fijo necesita el día de la semana.");
        }
        else
        {
            if (dto.Fecha is null)
                throw new ReglaDeNegocioException("Un bloqueo por rango necesita la fecha.");
            if (dto.Motivo is null)
                throw new ReglaDeNegocioException("Un bloqueo por rango necesita el motivo.");
            if (dto.Fecha < DateOnly.FromDateTime(DateTime.UtcNow))
                throw new ReglaDeNegocioException("La fecha del bloqueo no puede ser pasada.");
        }
    }

    private async Task<IReadOnlyList<Turno>> TurnosPisadosAsync(CreateBloqueoDto dto, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var candidatos = await _turnos.ListarProgramadosDesdeAsync(hoy, dto.CanchaId, ct);

        // Cubre() necesita la entidad; armamos una efímera con los datos del DTO
        var bloqueo = new Bloqueo
        {
            Tipo = dto.Tipo,
            Dia = dto.Dia,
            Fecha = dto.Fecha,
            HoraInicio = dto.HoraInicio,
            HoraFin = dto.HoraFin,
            CanchaId = dto.CanchaId,
        };
        return candidatos
            .Where(t => Cubre(bloqueo, t.Fecha, t.HoraInicio, t.DuracionMinutos, t.CanchaId))
            .ToList();
    }

    private static ImpactoBloqueoDto ArmarImpacto(IReadOnlyList<Turno> pisados) => new()
    {
        TurnosAfectados = pisados.Count,
        Afectados = pisados
            .OrderBy(t => t.Fecha).ThenBy(t => t.HoraInicio)
            .SelectMany(t => t.Participantes.Select(p => new AlumnoAfectadoDto
            {
                Fecha = t.Fecha,
                HoraInicio = t.HoraInicio,
                Titulo = Titulo(t),
                AlumnoNombre = p.Alumno is not null ? $"{p.Alumno.Nombre} {p.Alumno.Apellido}" : string.Empty,
                Telefono = p.Alumno?.Telefono,
            }))
            .ToList(),
    };

    // Mismo criterio que TurnoService.Mapear
    private static string Titulo(Turno t) =>
        t.Horario?.Grupo?.Nombre
            ?? (t.Horario?.Alumno is not null
                ? $"{t.Horario.Alumno.Nombre} {t.Horario.Alumno.Apellido} (individual)"
                : string.Empty);

    private static string MotivoLegible(MotivoBloqueo motivo) => motivo switch
    {
        MotivoBloqueo.MalClima => "Mal clima",
        MotivoBloqueo.MotivosPersonales => "Motivos personales",
        MotivoBloqueo.Torneo => "Torneo",
        MotivoBloqueo.MantenimientoCancha => "Mantenimiento de cancha",
        _ => motivo.ToString(),
    };

    private static BloqueoResponseDto Mapear(Bloqueo b) => new()
    {
        Id = b.Id,
        Tipo = b.Tipo.ToString(),
        Dia = b.Dia?.ToString(),
        Fecha = b.Fecha,
        HoraInicio = b.HoraInicio,
        HoraFin = b.HoraFin,
        CanchaId = b.CanchaId,
        Cancha = b.Cancha?.Nombre,
        Motivo = b.Motivo is not null ? MotivoLegible(b.Motivo.Value) : null,
        CreadoEl = b.CreadoEl,
    };
}
