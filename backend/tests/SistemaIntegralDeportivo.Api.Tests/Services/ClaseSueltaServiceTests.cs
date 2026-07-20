using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Clase suelta (M5c, TDD): el alumno reserva una clase individual en una FECHA
/// puntual; al pedir nace el cargo (precio individual); el profe confirma
/// (nace el turno suelto, se marca pagado) o rechaza (se borra el cargo).
/// </summary>
public class ClaseSueltaServiceTests
{
    private const decimal ValorIndividual = 16_000m;
    private static readonly Guid AlumnoId = Guid.NewGuid();
    private static readonly Guid SedeId = Guid.NewGuid();
    private static readonly Guid CanchaId = Guid.NewGuid();
    private static readonly DateOnly Manana = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);

    private readonly Mock<IClaseSueltaRepository> _clases;
    private readonly Mock<IAlumnoRepository> _alumnos;
    private readonly Mock<ISedeRepository> _sedes;
    private readonly Mock<IHorarioRepository> _horarios;
    private readonly Mock<ITurnoRepository> _turnos;
    private readonly Mock<ICargoRepository> _cargos;
    private readonly Mock<ITenantRepository> _tenant;
    private readonly ClaseSueltaService _service;

    public ClaseSueltaServiceTests()
    {
        _clases = new Mock<IClaseSueltaRepository>();
        _alumnos = new Mock<IAlumnoRepository>();
        _sedes = new Mock<ISedeRepository>();
        _horarios = new Mock<IHorarioRepository>();
        _turnos = new Mock<ITurnoRepository>();
        _cargos = new Mock<ICargoRepository>();
        _tenant = new Mock<ITenantRepository>();
        _service = new ClaseSueltaService(
            _clases.Object, _alumnos.Object, _sedes.Object, _horarios.Object,
            _turnos.Object, _cargos.Object, _tenant.Object);

        _alumnos.Setup(a => a.ObtenerAsync(AlumnoId, It.IsAny<CancellationToken>())).ReturnsAsync(AlumnoActivo());
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        _sedes.Setup(s => s.ObtenerAsync(SedeId, It.IsAny<CancellationToken>())).ReturnsAsync(SedeConCancha());
        _tenant.Setup(t => t.ObtenerActualAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Tenant { Subdominio = "d", Nombre = "Demo", ValorClaseIndividual = ValorIndividual });
        _horarios.Setup(h => h.ListarPorCanchaYDiaAsync(It.IsAny<Guid>(), It.IsAny<DayOfWeek>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);
        _turnos.Setup(t => t.ListarEntreAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
    }

    private static Alumno AlumnoActivo() => new()
    {
        Id = AlumnoId, Nombre = "Lucas", Apellido = "C", Dni = "1", Telefono = "1",
        FechaNacimiento = DateTime.UtcNow.AddYears(-25), Estado = EstadoAlumno.Activo,
    };

    private static Sede SedeConCancha()
    {
        var sede = new Sede { Id = SedeId, Nombre = "Central", Activo = true };
        sede.Canchas.Add(new Cancha { Id = CanchaId, Nombre = "Cancha 1", Activo = true });
        return sede;
    }

    // ── Solicitar ──

    [Fact]
    public async Task Solicitar_CreaLaClaseYElCargoConPrecioIndividual()
    {
        Cargo? cargo = null;
        ClaseSuelta? clase = null;
        _cargos.Setup(c => c.AgregarAsync(It.IsAny<Cargo>(), It.IsAny<CancellationToken>()))
               .Callback((Cargo c, CancellationToken _) => cargo = c).Returns(Task.CompletedTask);
        _clases.Setup(c => c.AgregarAsync(It.IsAny<ClaseSuelta>(), It.IsAny<CancellationToken>()))
               .Callback((ClaseSuelta c, CancellationToken _) => clase = c).Returns(Task.CompletedTask);

        var dto = await _service.SolicitarAsync(AlumnoId, SedeId, Manana, new TimeOnly(18, 0), 60);

        Assert.Equal("Pendiente", dto.Estado);
        Assert.NotNull(cargo);
        Assert.Equal(TipoCargo.Clase, cargo!.Tipo);
        Assert.Equal(16_000m, cargo.Monto);       // individual entero (60')
        Assert.Null(cargo.PagadoEl);              // impago: lo paga el alumno
        Assert.NotNull(clase);
        Assert.Equal(cargo.Id, clase!.CargoId);   // la clase apunta a su cargo
        Assert.Equal(SedeId, clase.SedeId);
    }

    [Fact]
    public async Task Solicitar_De30Min_ProrrateaElPrecio()
    {
        Cargo? cargo = null;
        _cargos.Setup(c => c.AgregarAsync(It.IsAny<Cargo>(), It.IsAny<CancellationToken>()))
               .Callback((Cargo c, CancellationToken _) => cargo = c).Returns(Task.CompletedTask);

        await _service.SolicitarAsync(AlumnoId, SedeId, Manana, new TimeOnly(18, 0), 30);

        Assert.Equal(8_000m, cargo!.Monto); // 16.000 × 30/60
    }

    [Fact]
    public async Task Solicitar_FechaPasada_Lanza()
    {
        var ayer = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.SolicitarAsync(AlumnoId, SedeId, ayer, new TimeOnly(18, 0), 60));
    }

    [Fact]
    public async Task Solicitar_SinCanchasLibresEnLaSede_Lanza()
    {
        _horarios.Setup(h => h.ListarPorCanchaYDiaAsync(CanchaId, Manana.DayOfWeek, It.IsAny<CancellationToken>()))
                 .ReturnsAsync([new Horario { CanchaId = CanchaId, Dia = Manana.DayOfWeek, HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60 }]);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.SolicitarAsync(AlumnoId, SedeId, Manana, new TimeOnly(18, 30), 60));

        _cargos.Verify(c => c.AgregarAsync(It.IsAny<Cargo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Solicitar_SinPrecioIndividualConfigurado_Lanza()
    {
        _tenant.Setup(t => t.ObtenerActualAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Tenant { Subdominio = "d", Nombre = "Demo" }); // sin precio

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.SolicitarAsync(AlumnoId, SedeId, Manana, new TimeOnly(18, 0), 60));
    }

    [Fact]
    public async Task Solicitar_ConDeudaVencida_Lanza()
    {
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([new Cargo { AlumnoId = AlumnoId, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m, Fecha = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2) }]);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.SolicitarAsync(AlumnoId, SedeId, Manana, new TimeOnly(18, 0), 60));
    }

    // ── Canchas libres (por fecha) ──

    [Fact]
    public async Task CanchasLibres_ExcluyeLaOcupadaPorUnSueltoDeEsaFecha()
    {
        // Otra clase suelta ya ocupa la Cancha 1 esa fecha 18:00
        _turnos.Setup(t => t.ListarEntreAsync(Manana, Manana, It.IsAny<CancellationToken>()))
               .ReturnsAsync([new Turno { CanchaId = CanchaId, Fecha = Manana, HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60, Estado = EstadoTurno.Programado }]);

        var libres = await _service.CanchasLibresAsync(SedeId, Manana, new TimeOnly(18, 30), 60);

        Assert.Empty(libres);
    }

    // ── Confirmar / Rechazar (profe) ──

    private ClaseSuelta PendienteConCargo()
    {
        var cargo = new Cargo { AlumnoId = AlumnoId, Tipo = TipoCargo.Clase, Concepto = "Clase suelta", Monto = 16_000m, Fecha = Manana };
        var clase = new ClaseSuelta { AlumnoId = AlumnoId, SedeId = SedeId, Fecha = Manana, HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60, CargoId = cargo.Id, Cargo = cargo, Estado = EstadoClaseSuelta.Pendiente };
        _clases.Setup(c => c.ObtenerAsync(clase.Id, It.IsAny<CancellationToken>())).ReturnsAsync(clase);
        return clase;
    }

    [Fact]
    public async Task Confirmar_CreaElTurnoSuelto_YMarcaPagadoElCargo()
    {
        var clase = PendienteConCargo();
        Turno? creado = null;
        _turnos.Setup(t => t.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()))
               .Callback((Turno t, CancellationToken _) => creado = t).Returns(Task.CompletedTask);

        await _service.ConfirmarAsync(clase.Id, CanchaId);

        Assert.NotNull(creado);
        Assert.Null(creado!.HorarioId);                    // turno SUELTO
        Assert.Equal(CanchaId, creado.CanchaId);
        Assert.Contains(creado.Participantes, p => p.AlumnoId == AlumnoId);
        Assert.NotNull(clase.Cargo!.PagadoEl);             // el cargo queda pagado
        Assert.Equal(creado.Id, clase.Cargo.TurnoId);      // linkeado al turno
        Assert.Equal(EstadoClaseSuelta.Confirmada, clase.Estado);
        Assert.Equal(creado.Id, clase.TurnoId);
    }

    [Fact]
    public async Task Confirmar_CanchaOcupada_Lanza_YNoCreaTurno()
    {
        var clase = PendienteConCargo();
        // La cancha elegida está ocupada por un recurrente a esa hora
        _horarios.Setup(h => h.ListarPorCanchaYDiaAsync(CanchaId, Manana.DayOfWeek, It.IsAny<CancellationToken>()))
                 .ReturnsAsync([new Horario { CanchaId = CanchaId, Dia = Manana.DayOfWeek, HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60 }]);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.ConfirmarAsync(clase.Id, CanchaId));

        _turnos.Verify(t => t.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(EstadoClaseSuelta.Pendiente, clase.Estado);
    }

    [Fact]
    public async Task Confirmar_YaResuelta_Lanza()
    {
        var clase = PendienteConCargo();
        clase.Estado = EstadoClaseSuelta.Confirmada;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.ConfirmarAsync(clase.Id, CanchaId));
    }

    [Fact]
    public async Task Rechazar_BorraElCargo_YMarcaRechazada()
    {
        var clase = PendienteConCargo();

        await _service.RechazarAsync(clase.Id);

        _cargos.Verify(c => c.Eliminar(clase.Cargo!), Times.Once);
        Assert.Equal(EstadoClaseSuelta.Rechazada, clase.Estado);
    }

    [Fact]
    public async Task InformarPago_MarcaElCargoComoInformado()
    {
        var clase = PendienteConCargo();

        await _service.InformarPagoAsync(AlumnoId, clase.Id);

        Assert.NotNull(clase.Cargo!.PagoInformadoEl);
        Assert.Null(clase.Cargo.PagadoEl); // informado ≠ pagado
    }

    [Fact]
    public async Task InformarPago_DeOtroAlumno_Lanza()
    {
        var clase = PendienteConCargo();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.InformarPagoAsync(Guid.NewGuid(), clase.Id));
    }
}
