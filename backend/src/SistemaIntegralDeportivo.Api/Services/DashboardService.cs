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
    // Orden fijo del ranking: varones (1ra→6ta), damas (A→D) y sin categoría
    private static readonly CategoriaAlumno[] OrdenCategorias =
    [
        CategoriaAlumno.Primera, CategoriaAlumno.Segunda, CategoriaAlumno.Tercera,
        CategoriaAlumno.Cuarta, CategoriaAlumno.Quinta, CategoriaAlumno.Sexta,
        CategoriaAlumno.A, CategoriaAlumno.B1, CategoriaAlumno.B2,
        CategoriaAlumno.C1, CategoriaAlumno.C2, CategoriaAlumno.D,
        CategoriaAlumno.SinCategoria,
    ];

    private const int CancelacionesAMostrar = 5;

    private readonly IAlumnoRepository _alumnos;
    private readonly ITurnoRepository _turnos;
    private readonly ICargoRepository _cargos;
    private readonly ITurnoService _turnoService;
    private readonly ICancelacionService _cancelaciones;

    public DashboardService(
        IAlumnoRepository alumnos, ITurnoRepository turnos,
        ICargoRepository cargos, ITurnoService turnoService,
        ICancelacionService cancelaciones)
    {
        _alumnos = alumnos;
        _turnos = turnos;
        _cargos = cargos;
        _turnoService = turnoService;
        _cancelaciones = cancelaciones;
    }

    public async Task<DashboardResumenDto> ObtenerResumenAsync(CancellationToken ct = default)
    {
        var ahora = DateTime.UtcNow;
        var hoy = DateOnly.FromDateTime(ahora);
        var inicioDeMes = new DateTime(ahora.Year, ahora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // "Clases de hoy" no puede depender de haber paseado por el Calendario, así
        // que si no hay nada materializado se genera el mes acá mismo (idempotente,
        // mismo patrón que Portal y Cuotas). Se pregunta PRIMERO porque casi siempre
        // ya están generados: generar de entrada eran tres idas a la base al pedo en
        // cada carga del inicio, y cada una cuesta ~115 ms.
        var turnosHoy = await _turnos.ListarEntreAsync(hoy, hoy, ct);
        if (turnosHoy.Count == 0)
        {
            await _turnoService.GenerarTurnosDelMesAsync(hoy.Year, hoy.Month, ct);
            turnosHoy = await _turnos.ListarEntreAsync(hoy, hoy, ct);
        }

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

        // Los cuatro números de alumnos salen de UNA sola consulta y se cuentan acá:
        // eran cuatro idas a la misma tabla, con el mismo dato leído cuatro veces.
        var fichas = await _alumnos.ResumenAsync(ct);
        var conClase = fichas.Where(f => f.TieneClase).ToList();

        return new DashboardResumenDto
        {
            // "Mis alumnos" son los que tienen clase: contar todos los activos metería
            // en el número a los que todavía están esperando que les asignen una.
            AlumnosActivos = conClase.Count(f => f.Estado == EstadoAlumno.Activo),
            NuevosEsteMes = fichas.Count(f => f.CreadoEl >= inicioDeMes),
            Pausados = fichas.Count(f => f.Estado == EstadoAlumno.Suspendido),
            RecaudacionDelMes = cargosMes.Where(c => c.PagadoEl is not null).Sum(c => c.Monto),
            // El desglose cuenta a los que tienen clase y no están de baja, para que
            // sume lo mismo que el total de arriba (si contara también a los que
            // esperan, el dashboard mostraría dos números que no cierran).
            PorCategoria = OrdenCategorias
                .Select(c => new CategoriaConteoDto
                {
                    Categoria = c.ToString(),
                    Cantidad = conClase.Count(f =>
                        f.Categoria == c && f.Estado != EstadoAlumno.Inactivo),
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
                    Cancha = t.Cancha,
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
            CancelacionesRecientes =
                [.. await _cancelaciones.ListarRecientesAsync(CancelacionesAMostrar, ct)],
        };
    }

    // Una sola definición del título, en TurnoService: las copias sueltas se
    // desactualizaron cuando el grupo dejó de existir.
    private static string Titulo(TurnoAgenda t) => TurnoService.TituloDe(t);
}
