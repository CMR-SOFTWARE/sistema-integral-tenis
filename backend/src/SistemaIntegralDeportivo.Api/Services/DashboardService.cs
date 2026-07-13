using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Compone agregados de los repositorios en el resumen del dashboard.
/// Sin reglas de negocio propias (composición y mapeo; el estado de cuota
/// lo decide CuotaService.CalcularEstado) → sin test-first, según ADR-0005.
/// </summary>
public class DashboardService : IDashboardService
{
    // Orden fijo del ranking: de la mejor (1ra) a la inicial (7ma) + sin categoría
    private static readonly CategoriaAlumno[] OrdenCategorias =
    [
        CategoriaAlumno.Primera, CategoriaAlumno.Segunda, CategoriaAlumno.Tercera,
        CategoriaAlumno.Cuarta, CategoriaAlumno.Quinta, CategoriaAlumno.Sexta,
        CategoriaAlumno.Septima, CategoriaAlumno.SinCategoria,
    ];

    private const int CancelacionesAMostrar = 5;

    private readonly IAlumnoRepository _alumnos;
    private readonly ITurnoRepository _turnos;
    private readonly ICargoRepository _cargos;
    private readonly ITurnoService _turnoService;

    public DashboardService(
        IAlumnoRepository alumnos, ITurnoRepository turnos,
        ICargoRepository cargos, ITurnoService turnoService)
    {
        _alumnos = alumnos;
        _turnos = turnos;
        _cargos = cargos;
        _turnoService = turnoService;
    }

    public async Task<DashboardResumenDto> ObtenerResumenAsync(CancellationToken ct = default)
    {
        var ahora = DateTime.UtcNow;
        var hoy = DateOnly.FromDateTime(ahora);
        var inicioDeMes = new DateTime(ahora.Year, ahora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Materializar los turnos del mes para que "clases de hoy" no dependa
        // de haber paseado por el Calendario (idempotente, mismo patrón que
        // Portal y Cuotas)
        await _turnoService.GenerarTurnosDelMesAsync(hoy.Year, hoy.Month, ct);
        var turnosHoy = await _turnos.ListarEntreAsync(hoy, hoy, ct);

        // Cuotas del mes en curso: SOLO cargos ya materializados — la
        // liquidación (que genera cargos y exige precios configurados) se
        // dispara desde la pantalla Cuotas, no desde un GET de dashboard
        var cargosMes = await _cargos.ListarDelMesAsync(hoy.Year, hoy.Month, ct);
        var porAlumno = cargosMes
            .GroupBy(c => c.AlumnoId)
            .Select(g => new
            {
                Saldo = g.Sum(c => c.Monto) - g.Where(c => c.PagadoEl is not null).Sum(c => c.Monto),
            })
            .ToList();
        var estados = porAlumno
            .Where(a => a.Saldo > 0)
            .Select(a => CuotaService.CalcularEstado(hoy.Year, hoy.Month, a.Saldo, hoy))
            .ToList();

        var cancelados = await _turnos.ListarCanceladosRecientesAsync(CancelacionesAMostrar, ct);
        var porCategoria = await _alumnos.ContarPorCategoriaAsync(ct);

        return new DashboardResumenDto
        {
            AlumnosActivos = await _alumnos.ContarPorEstadoAsync(EstadoAlumno.Activo, ct),
            NuevosEsteMes = await _alumnos.ContarNuevosDesdeAsync(inicioDeMes, ct),
            Pausados = await _alumnos.ContarPorEstadoAsync(EstadoAlumno.Suspendido, ct),
            RecaudacionDelMes = cargosMes.Where(c => c.PagadoEl is not null).Sum(c => c.Monto),
            PorCategoria = OrdenCategorias
                .Select(c => new CategoriaConteoDto
                {
                    Categoria = c.ToString(),
                    Cantidad = porCategoria.GetValueOrDefault(c),
                })
                .ToList(),
            ClasesHoy = turnosHoy
                .OrderBy(t => t.HoraInicio)
                .Select(t => new ClaseHoyDto
                {
                    TurnoId = t.Id,
                    HoraInicio = t.HoraInicio,
                    DuracionMinutos = t.DuracionMinutos,
                    Titulo = Titulo(t),
                    Cancha = t.Cancha?.Nombre ?? string.Empty,
                    Participantes = t.Participantes.Count,
                    Estado = t.Estado.ToString(),
                })
                .ToList(),
            CuotasPendientes = new CuotasPendientesDto
            {
                AlumnosPendientes = estados.Count(e => e == "Pendiente"),
                AlumnosVencidos = estados.Count(e => e == "Vencida"),
                TotalPendiente = porAlumno.Where(a => a.Saldo > 0).Sum(a => a.Saldo),
            },
            CancelacionesRecientes = cancelados
                .Select(t => new CancelacionRecienteDto
                {
                    Fecha = t.Fecha,
                    HoraInicio = t.HoraInicio,
                    Titulo = Titulo(t),
                    Motivo = t.CanceladoMotivo,
                    CanceladoEl = t.CanceladoEl,
                })
                .ToList(),
        };
    }

    // Mismo criterio que TurnoService.Mapear: nombre del grupo o "Fulano (individual)"
    private static string Titulo(Turno t) =>
        t.Horario?.Grupo?.Nombre
            ?? (t.Horario?.Alumno is not null
                ? $"{t.Horario.Alumno.Nombre} {t.Horario.Alumno.Apellido} (individual)"
                : string.Empty);
}
