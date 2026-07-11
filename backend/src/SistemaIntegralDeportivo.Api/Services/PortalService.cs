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

    public PortalService(
        IAlumnoRepository alumnos, ITurnoRepository turnos,
        ITurnoService turnoService, ICuotaService cuotas)
    {
        _alumnos = alumnos;
        _turnos = turnos;
        _turnoService = turnoService;
        _cuotas = cuotas;
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

    private async Task<Alumno> FichaDeAsync(Guid userId, CancellationToken ct) =>
        await _alumnos.ObtenerPorUserIdAsync(userId, ct)
            ?? throw new ReglaDeNegocioException(
                "Tu cuenta no está vinculada a ningún club todavía. Reclamá tu ficha desde el inicio.");

    private static MiTurnoDto Mapear(Turno t, Guid miAlumnoId) => new()
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
        Presente = t.Participantes.FirstOrDefault(p => p.AlumnoId == miAlumnoId)?.Presente ?? true,
        Companeros = t.Participantes
            .Where(p => p.AlumnoId != miAlumnoId && p.Alumno is not null)
            .Select(p => $"{p.Alumno!.Nombre} {p.Alumno.Apellido}")
            .ToList(),
    };
}
