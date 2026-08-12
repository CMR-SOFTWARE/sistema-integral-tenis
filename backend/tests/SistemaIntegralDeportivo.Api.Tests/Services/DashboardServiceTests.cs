using Moq;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// El dashboard no tiene reglas propias (ADR-0005: sin test-first), pero sí decide
/// QUIÉN entra en cada número, y esos criterios se equivocan fácil: se movieron acá
/// desde cuatro queries del repositorio, que se fusionaron en una sola.
///
/// El otro test que importa es de forma, no de resultado: que no genere los turnos
/// del mes cuando ya están, porque eso es lo que le sacó tres idas a la base.
/// </summary>
public class DashboardServiceTests
{
    private readonly Mock<IAlumnoRepository> _alumnos = new();
    private readonly Mock<ITurnoRepository> _turnos = new();
    private readonly Mock<ICargoRepository> _cargos = new();
    private readonly Mock<ITurnoService> _turnoService = new();
    private readonly Mock<ICancelacionService> _cancelaciones = new();
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        _service = new DashboardService(
            _alumnos.Object, _turnos.Object, _cargos.Object,
            _turnoService.Object, _cancelaciones.Object);

        // Por defecto: hay una clase hoy, sin cargos ni cancelaciones, y ningún alumno.
        _turnos.Setup(t => t.ListarEntreAsync(
                   It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([new Turno { Fecha = Hoy, HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60 }]);
        _cargos.Setup(c => c.ListarDelMesAsync(
                   It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        _cancelaciones.Setup(c => c.ListarRecientesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync([]);
        Fichas();
    }

    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.UtcNow);

    private void Fichas(params AlumnoResumenFila[] filas) =>
        _alumnos.Setup(a => a.ResumenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(filas);

    /// <summary>Alumno con clase, de alta hace un año (no cuenta como nuevo del mes).</summary>
    private static AlumnoResumenFila Con(
        EstadoAlumno estado = EstadoAlumno.Activo,
        CategoriaAlumno categoria = CategoriaAlumno.Cuarta,
        bool tieneClase = true,
        DateTime? creadoEl = null) =>
        new(estado, categoria, creadoEl ?? DateTime.UtcNow.AddYears(-1), tieneClase);

    // ── Quién entra en cada número ──

    [Fact]
    public async Task AlumnosActivos_CuentaSoloLosActivosConClase()
    {
        Fichas(
            Con(),                                              // ✓
            Con(tieneClase: false),                             // espera: no es alumno
            Con(estado: EstadoAlumno.Suspendido),               // pausado
            Con(estado: EstadoAlumno.Inactivo));                // baja

        var resumen = await _service.ObtenerResumenAsync();

        Assert.Equal(1, resumen.AlumnosActivos);
    }

    [Fact]
    public async Task NuevosEsteMes_CuentaLasAltasDelMes_TenganClaseONo()
    {
        // El alta es alta aunque todavía no le hayan asignado clase.
        Fichas(
            Con(creadoEl: DateTime.UtcNow),                      // ✓
            Con(creadoEl: DateTime.UtcNow, tieneClase: false),   // ✓ recién llegado
            Con());                                             // de hace un año

        var resumen = await _service.ObtenerResumenAsync();

        Assert.Equal(2, resumen.NuevosEsteMes);
    }

    [Fact]
    public async Task Pausados_CuentaLosSuspendidos_AunqueNoTenganClase()
    {
        Fichas(
            Con(estado: EstadoAlumno.Suspendido),
            Con(estado: EstadoAlumno.Suspendido, tieneClase: false),
            Con(estado: EstadoAlumno.Inactivo));

        var resumen = await _service.ObtenerResumenAsync();

        Assert.Equal(2, resumen.Pausados);
    }

    [Fact]
    public async Task PorCategoria_SumaLoMismoQueElTotal()
    {
        // El desglose tiene que cerrar con "mis alumnos": si contara a los que esperan
        // o a las bajas, el profe vería dos números que no dan.
        Fichas(
            Con(categoria: CategoriaAlumno.Cuarta),
            Con(categoria: CategoriaAlumno.Cuarta),
            Con(categoria: CategoriaAlumno.Primera),
            Con(categoria: CategoriaAlumno.Primera, tieneClase: false),
            Con(categoria: CategoriaAlumno.Primera, estado: EstadoAlumno.Inactivo));

        var resumen = await _service.ObtenerResumenAsync();

        Assert.Equal(2, resumen.PorCategoria.Single(c => c.Categoria == "Cuarta").Cantidad);
        Assert.Equal(1, resumen.PorCategoria.Single(c => c.Categoria == "Primera").Cantidad);
        Assert.Equal(resumen.AlumnosActivos, resumen.PorCategoria.Sum(c => c.Cantidad));
    }

    [Fact]
    public async Task Alumnos_SePidenUnaSolaVez()
    {
        await _service.ObtenerResumenAsync();

        _alumnos.Verify(a => a.ResumenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── La generación perezosa: solo cuando hace falta ──

    [Fact]
    public async Task ConClasesGeneradas_NoVuelveAGenerarElMes()
    {
        await _service.ObtenerResumenAsync();

        _turnoService.Verify(t => t.GenerarTurnosDelMesAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SinNadaGenerado_GeneraElMesYVuelveAMirar()
    {
        // Primer acceso del mes sin haber pasado por la Agenda: las clases de hoy
        // tienen que aparecer igual.
        var recienGenerado = new Turno { Fecha = Hoy, HoraInicio = new TimeOnly(9, 0), DuracionMinutos = 60 };
        _turnos.SetupSequence(t => t.ListarEntreAsync(
                   It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([])
               .ReturnsAsync([recienGenerado]);

        var resumen = await _service.ObtenerResumenAsync();

        _turnoService.Verify(t => t.GenerarTurnosDelMesAsync(
            Hoy.Year, Hoy.Month, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(resumen.ClasesHoy);
    }
}
