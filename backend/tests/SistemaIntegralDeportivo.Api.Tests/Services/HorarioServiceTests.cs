using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Reglas de horarios (TDD): solapamiento POR CANCHA, grupal XOR individual,
/// y desactivación que limpia el futuro sin tocar historia ni plata cobrada.
/// </summary>
public class HorarioServiceTests
{
    private static readonly Guid Cancha1 = Guid.NewGuid();
    private static readonly Guid Cancha2 = Guid.NewGuid();
    private static readonly Guid GrupoId = Guid.NewGuid();
    private static readonly Guid AlumnoId = Guid.NewGuid();

    private readonly Mock<IHorarioRepository> _repo;
    private readonly Mock<ITurnoRepository> _turnos;
    private readonly Mock<ICargoRepository> _cargos;
    private readonly HorarioService _service;

    public HorarioServiceTests()
    {
        _repo = new Mock<IHorarioRepository>();
        _turnos = new Mock<ITurnoRepository>();
        _cargos = new Mock<ICargoRepository>();
        _service = new HorarioService(_repo.Object, _turnos.Object, _cargos.Object);

        // Por defecto: nadie debe nada
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);

        // Ya existe: martes 18:00-19:00 en Cancha 1
        var existente = new Horario
        {
            CanchaId = Cancha1,
            GrupoId = GrupoId,
            Dia = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(18, 0),
            DuracionMinutos = 60,
        };
        _repo.Setup(r => r.ListarPorCanchaYDiaAsync(Cancha1, DayOfWeek.Tuesday, It.IsAny<CancellationToken>()))
             .ReturnsAsync([existente]);
        _repo.Setup(r => r.ListarPorCanchaYDiaAsync(Cancha2, It.IsAny<DayOfWeek>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Horario h, CancellationToken _) => h);
    }

    private static CreateHorarioDto Dto(Guid cancha, TimeOnly hora, int duracion = 60) => new()
    {
        CanchaId = cancha,
        GrupoId = GrupoId,
        Dia = DayOfWeek.Tuesday,
        HoraInicio = hora,
        DuracionMinutos = duracion,
    };

    [Fact]
    public async Task Crear_SolapaEnLaMismaCancha_Lanza()
    {
        // 18:30-19:30 pisa al de 18:00-19:00 en la misma cancha
        var dto = Dto(Cancha1, new TimeOnly(18, 30));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Crear_MismaHoraEnOtraCancha_Crea()
    {
        // El profe tiene staff: dos clases a la vez en canchas distintas, OK
        var dto = Dto(Cancha2, new TimeOnly(18, 0));

        await _service.CrearAsync(dto);

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crear_ContiguoEnLaMismaCancha_NoEsSolapamiento()
    {
        // 19:00-20:00 arranca justo cuando termina el de 18:00 → válido
        var dto = Dto(Cancha1, new TimeOnly(19, 0));

        await _service.CrearAsync(dto);

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crear_SinGrupoNiAlumno_Lanza()
    {
        var dto = Dto(Cancha2, new TimeOnly(10, 0));
        dto.GrupoId = null; // ni grupo ni alumno

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
    }

    [Fact]
    public async Task Crear_ConGrupoYAlumnoALaVez_Lanza()
    {
        var dto = Dto(Cancha2, new TimeOnly(10, 0));
        dto.AlumnoId = AlumnoId; // grupo Y alumno: ambiguo

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
    }

    [Fact]
    public async Task Crear_IndividualSinDeuda_Crea()
    {
        var dto = Dto(Cancha2, new TimeOnly(10, 0));
        dto.GrupoId = null;
        dto.AlumnoId = AlumnoId;

        await _service.CrearAsync(dto);

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crear_IndividualConCuotaVencida_Lanza()
    {
        var dto = Dto(Cancha2, new TimeOnly(10, 0));
        dto.GrupoId = null;
        dto.AlumnoId = AlumnoId;
        // Debe una clase de hace 2 meses: liquidación vencida hace rato
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([new Cargo
               {
                   AlumnoId = AlumnoId, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m,
                   Fecha = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2),
               }]);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─────────────────────────────────────────────
    // Desactivar: apaga la plantilla y limpia el futuro
    // ─────────────────────────────────────────────

    private Horario HorarioActivo()
    {
        var horario = new Horario
        {
            CanchaId = Cancha1,
            AlumnoId = AlumnoId,
            Dia = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(10, 0),
            DuracionMinutos = 30,
            Activo = true,
        };
        _repo.Setup(r => r.ObtenerAsync(horario.Id, It.IsAny<CancellationToken>()))
             .ReturnsAsync(horario);
        return horario;
    }

    private Turno TurnoDe(Horario horario, DateOnly fecha) => new()
    {
        HorarioId = horario.Id,
        CanchaId = horario.CanchaId,
        Fecha = fecha,
        HoraInicio = horario.HoraInicio,
        DuracionMinutos = horario.DuracionMinutos,
    };

    [Fact]
    public async Task Desactivar_ApagaLaPlantilla_YBorraTurnosFuturosConSusCargosImpagos()
    {
        var horario = HorarioActivo();
        var futuro = TurnoDe(horario, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3));
        var cargoImpago = new Cargo
        {
            AlumnoId = AlumnoId, TurnoId = futuro.Id, Tipo = TipoCargo.Clase,
            Concepto = "Clase individual (30')", Monto = 8_000m, Fecha = futuro.Fecha,
        };
        _turnos.Setup(t => t.ListarPorHorarioDesdeAsync(horario.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([futuro]);
        _cargos.Setup(c => c.ListarPorTurnosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([cargoImpago]);

        await _service.DesactivarAsync(horario.Id);

        Assert.False(horario.Activo);
        _cargos.Verify(c => c.Eliminar(cargoImpago), Times.Once);
        _turnos.Verify(t => t.Eliminar(futuro), Times.Once);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Desactivar_ConservaElTurnoFuturoSiTieneUnCargoPagado()
    {
        var horario = HorarioActivo();
        var futuro = TurnoDe(horario, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3));
        var cargoPagado = new Cargo
        {
            AlumnoId = AlumnoId, TurnoId = futuro.Id, Tipo = TipoCargo.Clase,
            Concepto = "Clase individual (30')", Monto = 8_000m, Fecha = futuro.Fecha,
            PagadoEl = DateTime.UtcNow, MedioPago = MedioPago.Efectivo,
        };
        _turnos.Setup(t => t.ListarPorHorarioDesdeAsync(horario.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([futuro]);
        _cargos.Setup(c => c.ListarPorTurnosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([cargoPagado]);

        await _service.DesactivarAsync(horario.Id);

        // Plata cobrada no se toca: ni el cargo ni su turno se borran
        Assert.False(horario.Activo);
        _cargos.Verify(c => c.Eliminar(It.IsAny<Cargo>()), Times.Never);
        _turnos.Verify(t => t.Eliminar(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task Desactivar_SoloMiraDesdeHoy_LoPasadoEsHistoria()
    {
        var horario = HorarioActivo();
        _turnos.Setup(t => t.ListarPorHorarioDesdeAsync(horario.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);

        await _service.DesactivarAsync(horario.Id);

        // Pide los turnos DESDE HOY: los anteriores ni se consultan
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        _turnos.Verify(t => t.ListarPorHorarioDesdeAsync(horario.Id, hoy, It.IsAny<CancellationToken>()), Times.Once);
        _turnos.Verify(t => t.Eliminar(It.IsAny<Turno>()), Times.Never);
    }
}
