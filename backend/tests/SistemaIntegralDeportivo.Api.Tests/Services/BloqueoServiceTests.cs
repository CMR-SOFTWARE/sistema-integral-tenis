using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Reglas de bloqueos (TDD): validación fijo/rango, cascada que cancela solo
/// los turnos programados que solapan (nadie paga: cargos impagos fuera,
/// pagados intocables) y preview de impacto que no persiste.
/// </summary>
public class BloqueoServiceTests
{
    private static readonly Guid Cancha1 = Guid.NewGuid();
    private static readonly Guid Cancha2 = Guid.NewGuid();
    private static readonly Guid AlumnoJuan = Guid.NewGuid();
    private static readonly Guid AlumnaSofia = Guid.NewGuid();

    // Una fecha futura fija y su día de semana, para tests determinísticos
    private static readonly DateOnly Futuro = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(14);

    private readonly Mock<IBloqueoRepository> _bloqueos;
    private readonly Mock<ITurnoRepository> _turnos;
    private readonly Mock<ICargoRepository> _cargos;
    private readonly BloqueoService _service;

    public BloqueoServiceTests()
    {
        _bloqueos = new Mock<IBloqueoRepository>();
        _turnos = new Mock<ITurnoRepository>();
        _cargos = new Mock<ICargoRepository>();
        _service = new BloqueoService(_bloqueos.Object, _turnos.Object, _cargos.Object);

        // Por defecto: agenda vacía, sin cargos
        _turnos.Setup(t => t.ListarProgramadosDesdeAsync(It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        _cargos.Setup(c => c.ListarPorTurnosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
    }

    private static CreateBloqueoDto RangoValido() => new()
    {
        Tipo = TipoBloqueo.Rango,
        Fecha = Futuro,
        HoraInicio = new TimeOnly(18, 0),
        HoraFin = new TimeOnly(20, 0),
        Motivo = MotivoBloqueo.MalClima,
    };

    private static CreateBloqueoDto FijoValido() => new()
    {
        Tipo = TipoBloqueo.Fijo,
        Dia = Futuro.DayOfWeek,
        HoraInicio = new TimeOnly(8, 0),
        HoraFin = new TimeOnly(12, 0),
    };

    /// <summary>Turno programado en Cancha1 con Juan y Sofía.</summary>
    private static Turno TurnoEn(DateOnly fecha, TimeOnly hora, int duracion = 60, Guid? cancha = null)
    {
        var turno = new Turno
        {
            CanchaId = cancha ?? Cancha1,
            Fecha = fecha,
            HoraInicio = hora,
            DuracionMinutos = duracion,
            Horario = new Horario
            {
                CanchaId = cancha ?? Cancha1,
                Dia = fecha.DayOfWeek,
                HoraInicio = hora,
                DuracionMinutos = duracion,
                Grupo = new Grupo { Nombre = "Intermedios" },
            },
        };
        turno.Participantes.Add(new TurnoParticipante
        {
            Turno = turno,
            AlumnoId = AlumnoJuan,
            Alumno = new Alumno
            {
                Id = AlumnoJuan, Nombre = "Juan", Apellido = "Pérez",
                Dni = "1", Telefono = "+5491111111111",
            },
        });
        turno.Participantes.Add(new TurnoParticipante
        {
            Turno = turno,
            AlumnoId = AlumnaSofia,
            Alumno = new Alumno
            {
                Id = AlumnaSofia, Nombre = "Sofía", Apellido = "Gómez",
                Dni = "2", Telefono = "+5492222222222",
            },
        });
        return turno;
    }

    // ─────────────────────────────────────────────
    // Validaciones de forma
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Crear_HoraFinAntesDeInicio_Lanza()
    {
        var dto = RangoValido();
        dto.HoraFin = new TimeOnly(17, 0); // antes de las 18

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
    }

    [Fact]
    public async Task Crear_FijoSinDia_Lanza()
    {
        var dto = FijoValido();
        dto.Dia = null;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
    }

    [Fact]
    public async Task Crear_RangoSinFecha_Lanza()
    {
        var dto = RangoValido();
        dto.Fecha = null;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
    }

    [Fact]
    public async Task Crear_RangoSinMotivo_Lanza()
    {
        var dto = RangoValido();
        dto.Motivo = null;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
    }

    [Fact]
    public async Task Crear_RangoConFechaPasada_Lanza()
    {
        var dto = RangoValido();
        dto.Fecha = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
    }

    // ─────────────────────────────────────────────
    // Cascada: cancela SOLO lo que el bloqueo pisa
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Crear_Rango_CancelaLosTurnosQueSolapan_YNoLosDemas()
    {
        var pisado = TurnoEn(Futuro, new TimeOnly(18, 0));          // 18-19, dentro de 18-20
        var otraHora = TurnoEn(Futuro, new TimeOnly(9, 0));         // 9-10, fuera
        var otraFecha = TurnoEn(Futuro.AddDays(1), new TimeOnly(18, 0));
        _turnos.Setup(t => t.ListarProgramadosDesdeAsync(It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([pisado, otraHora, otraFecha]);

        var creado = await _service.CrearAsync(RangoValido());

        Assert.Equal(EstadoTurno.Cancelado, pisado.Estado);
        Assert.Equal("Bloqueo: Mal clima", pisado.CanceladoMotivo);
        Assert.NotNull(pisado.CanceladoEl);
        Assert.Equal(EstadoTurno.Programado, otraHora.Estado);
        Assert.Equal(EstadoTurno.Programado, otraFecha.Estado);
        Assert.Equal(1, creado.Impacto.TurnosAfectados);
        _bloqueos.Verify(b => b.AgregarAsync(It.IsAny<Bloqueo>(), It.IsAny<CancellationToken>()), Times.Once);
        _bloqueos.Verify(b => b.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crear_SolapeParcial_TambienCancela()
    {
        // Turno 17:30-18:30 solapa media hora con el bloqueo 18-20
        var parcial = TurnoEn(Futuro, new TimeOnly(17, 30));
        _turnos.Setup(t => t.ListarProgramadosDesdeAsync(It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([parcial]);

        await _service.CrearAsync(RangoValido());

        Assert.Equal(EstadoTurno.Cancelado, parcial.Estado);
    }

    [Fact]
    public async Task Crear_TurnoQueTerminaJustoAlInicio_NoSeCancela()
    {
        // Turno 17:00-18:00: termina exactamente cuando arranca el bloqueo
        var justo = TurnoEn(Futuro, new TimeOnly(17, 0));
        _turnos.Setup(t => t.ListarProgramadosDesdeAsync(It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([justo]);

        var creado = await _service.CrearAsync(RangoValido());

        Assert.Equal(EstadoTurno.Programado, justo.Estado);
        Assert.Equal(0, creado.Impacto.TurnosAfectados);
    }

    [Fact]
    public async Task Crear_ConCanchaEspecifica_NoTocaOtrasCanchas()
    {
        var enCancha2 = TurnoEn(Futuro, new TimeOnly(18, 0), cancha: Cancha2);
        _turnos.Setup(t => t.ListarProgramadosDesdeAsync(It.IsAny<DateOnly>(), Cancha1, It.IsAny<CancellationToken>()))
               .ReturnsAsync([]); // el repo ya filtra por cancha
        var dto = RangoValido();
        dto.CanchaId = Cancha1;

        var creado = await _service.CrearAsync(dto);

        Assert.Equal(EstadoTurno.Programado, enCancha2.Estado);
        Assert.Equal(0, creado.Impacto.TurnosAfectados);
        // Y el repo fue consultado CON el filtro de cancha
        _turnos.Verify(t => t.ListarProgramadosDesdeAsync(It.IsAny<DateOnly>(), Cancha1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crear_Fijo_CancelaLosTurnosDelDiaQueSolapan()
    {
        // Bloqueo fijo 8-12 el día de semana de Futuro; turno ese día 9:00
        var pisado = TurnoEn(Futuro, new TimeOnly(9, 0));
        var otroDia = TurnoEn(Futuro.AddDays(1), new TimeOnly(9, 0));
        _turnos.Setup(t => t.ListarProgramadosDesdeAsync(It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([pisado, otroDia]);

        var creado = await _service.CrearAsync(FijoValido());

        Assert.Equal(EstadoTurno.Cancelado, pisado.Estado);
        Assert.Equal("Bloqueo fijo", pisado.CanceladoMotivo);
        Assert.Equal(EstadoTurno.Programado, otroDia.Estado);
        Assert.Equal(1, creado.Impacto.TurnosAfectados);
    }

    // ─────────────────────────────────────────────
    // Cargos: impagos fuera, pagados intocables
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Crear_EliminaCargosImpagosDelTurnoPisado_YRespetaPagados()
    {
        var pisado = TurnoEn(Futuro, new TimeOnly(18, 0));
        var impago = new Cargo
        {
            AlumnoId = AlumnoJuan, TurnoId = pisado.Id, Tipo = TipoCargo.Clase,
            Concepto = "x", Monto = 4_000m, Fecha = Futuro,
        };
        var pagado = new Cargo
        {
            AlumnoId = AlumnaSofia, TurnoId = pisado.Id, Tipo = TipoCargo.Clase,
            Concepto = "x", Monto = 4_000m, Fecha = Futuro,
            PagadoEl = DateTime.UtcNow, MedioPago = MedioPago.Efectivo,
        };
        _turnos.Setup(t => t.ListarProgramadosDesdeAsync(It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([pisado]);
        _cargos.Setup(c => c.ListarPorTurnosAsync(
                   It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(pisado.Id)), It.IsAny<CancellationToken>()))
               .ReturnsAsync([impago, pagado]);

        await _service.CrearAsync(RangoValido());

        _cargos.Verify(c => c.Eliminar(impago), Times.Once);
        _cargos.Verify(c => c.Eliminar(pagado), Times.Never);
    }

    // ─────────────────────────────────────────────
    // Preview: calcula sin persistir
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Impacto_ReportaTurnosYAlumnos_SinPersistirNada()
    {
        var pisado = TurnoEn(Futuro, new TimeOnly(18, 0));
        _turnos.Setup(t => t.ListarProgramadosDesdeAsync(It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([pisado]);

        var impacto = await _service.PrevisualizarImpactoAsync(RangoValido());

        Assert.Equal(1, impacto.TurnosAfectados);
        Assert.Equal(2, impacto.Afectados.Count);
        Assert.Contains(impacto.Afectados, a => a.AlumnoNombre == "Juan Pérez" && a.Telefono == "+5491111111111");
        Assert.Contains(impacto.Afectados, a => a.Titulo == "Intermedios");
        // Nada se tocó: ni el turno, ni el bloqueo, ni cargos
        Assert.Equal(EstadoTurno.Programado, pisado.Estado);
        _bloqueos.Verify(b => b.AgregarAsync(It.IsAny<Bloqueo>(), It.IsAny<CancellationToken>()), Times.Never);
        _bloqueos.Verify(b => b.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cargos.Verify(c => c.Eliminar(It.IsAny<Cargo>()), Times.Never);
    }

    [Fact]
    public async Task Impacto_TambienValida()
    {
        var dto = RangoValido();
        dto.Fecha = null;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.PrevisualizarImpactoAsync(dto));
    }

    // ─────────────────────────────────────────────
    // Eliminar
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Eliminar_BorraElBloqueo()
    {
        var bloqueo = new Bloqueo { Tipo = TipoBloqueo.Fijo, Dia = DayOfWeek.Monday };
        _bloqueos.Setup(b => b.ObtenerAsync(bloqueo.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(bloqueo);

        await _service.EliminarAsync(bloqueo.Id);

        _bloqueos.Verify(b => b.Eliminar(bloqueo), Times.Once);
        _bloqueos.Verify(b => b.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Eliminar_Inexistente_Lanza()
    {
        _bloqueos.Setup(b => b.ObtenerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Bloqueo?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.EliminarAsync(Guid.NewGuid()));
    }
}
