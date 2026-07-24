using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Reglas del portal alumno (TDD): todo se scopea a la FICHA VINCULADA al
/// usuario del token — sin ficha no hay portal, y nadie ve datos ajenos.
/// </summary>
public class PortalServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OtroAlumnoId = Guid.NewGuid();

    private readonly Mock<IAlumnoRepository> _alumnos;
    private readonly Mock<ITurnoRepository> _turnos;
    private readonly Mock<ITurnoService> _turnoService;
    private readonly Mock<ICuotaService> _cuotas;
    private readonly Mock<IServicioService> _servicios;
    private readonly Mock<IPedidoService> _pedidos;
    private readonly Mock<IRaquetaService> _raquetas;
    private readonly Mock<ISolicitudGrupoService> _solicitudesGrupo;
    private readonly Mock<ISolicitudHorarioService> _solicitudesHorario;
    private readonly Mock<IClaseSueltaService> _clasesSueltas;
    private readonly Mock<IPublicidadService> _publicidad;
    private readonly Mock<IAvisoService> _avisos;
    private readonly Mock<INotaAlumnoService> _notas;
    private readonly Mock<ISedeRepository> _sedes;
    private readonly Mock<ITenantActual> _tenantActual;
    private readonly Mock<IFichaActual> _fichaActual;
    private readonly PortalService _service;
    private readonly Alumno _ficha;

    public PortalServiceTests()
    {
        _alumnos = new Mock<IAlumnoRepository>();
        _turnos = new Mock<ITurnoRepository>();
        _turnoService = new Mock<ITurnoService>();
        _cuotas = new Mock<ICuotaService>();
        _servicios = new Mock<IServicioService>();
        _pedidos = new Mock<IPedidoService>();
        _raquetas = new Mock<IRaquetaService>();
        _solicitudesGrupo = new Mock<ISolicitudGrupoService>();
        _solicitudesHorario = new Mock<ISolicitudHorarioService>();
        _clasesSueltas = new Mock<IClaseSueltaService>();
        _publicidad = new Mock<IPublicidadService>();
        _avisos = new Mock<IAvisoService>();
        _notas = new Mock<INotaAlumnoService>();
        _sedes = new Mock<ISedeRepository>();
        _tenantActual = new Mock<ITenantActual>();
        _fichaActual = new Mock<IFichaActual>(); // por defecto AlumnoId = null → ficha default
        _service = new PortalService(
            _alumnos.Object, _turnos.Object, _turnoService.Object, _cuotas.Object,
            _servicios.Object, _pedidos.Object, _raquetas.Object, _solicitudesGrupo.Object,
            _solicitudesHorario.Object, _clasesSueltas.Object, _publicidad.Object, _avisos.Object,
            _notas.Object, _sedes.Object, _tenantActual.Object, _fichaActual.Object);
        _raquetas.Setup(r => r.MisAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);

        _ficha = new Alumno
        {
            TenantId = Guid.NewGuid(),
            Tenant = new Tenant { Subdominio = "demo", Nombre = "Club Demo" },
            Nombre = "Lucas",
            Apellido = "Calderón",
            Dni = "30111222",
            Telefono = "+549115555",
            UserId = UserId,
        };
        _alumnos.Setup(a => a.ObtenerPorUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_ficha);
        _turnos.Setup(t => t.ListarPorAlumnoEntreAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
    }

    [Fact]
    public async Task MisTurnos_SinFichaVinculada_Lanza()
    {
        var sinFicha = Guid.NewGuid();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.MisTurnosAsync(sinFicha));
    }

    [Fact]
    public async Task MisTurnos_FijaElTenantDelClubDeLaFicha()
    {
        // COSTURA ADR-0010: el alumno no trae claim tenant — el portal debe
        // fijar el tenant de SU club antes de generar turnos o liquidar
        await _service.MisTurnosAsync(UserId);

        _tenantActual.Verify(t => t.Establecer(_ficha.TenantId), Times.Once);
    }

    [Fact]
    public async Task MiCuotaFamilia_SumaSoloLosMiembros_YNoLosAjenos()
    {
        // Capa 2b: la cuota consolidada filtra a las fichas de la familia y suma
        var f1 = new Alumno { TenantId = Guid.NewGuid(), Nombre = "Sofía", Apellido = "Gómez", Telefono = "1", UserId = UserId };
        var f2 = new Alumno { TenantId = f1.TenantId, Nombre = "Juli", Apellido = "Gómez", Telefono = "1", UserId = UserId };
        _alumnos.Setup(a => a.ListarPorUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([f1, f2]);
        var ajeno = Guid.NewGuid();
        _cuotas.Setup(c => c.ObtenerMesAsync(2026, 7, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new LiquidacionMesDto
               {
                   Anio = 2026, Mes = 7,
                   Liquidaciones =
                   [
                       new AlumnoLiquidacionDto { AlumnoId = f1.Id, Total = 12000, Saldo = 12000, Estado = "Pendiente" },
                       new AlumnoLiquidacionDto { AlumnoId = f2.Id, Total = 10000, Saldo = 10000, Estado = "Pendiente" },
                       new AlumnoLiquidacionDto { AlumnoId = ajeno, Total = 99999, Saldo = 99999, Estado = "Pendiente" },
                   ],
               });

        var res = await _service.MiCuotaFamiliaAsync(UserId, 2026, 7);

        Assert.Equal(2, res.Miembros.Count);   // solo la familia, no el ajeno
        Assert.Equal(22000, res.Total);
        Assert.Equal(22000, res.Saldo);
        Assert.True(res.PuedeInformar);
        _tenantActual.Verify(t => t.Establecer(f1.TenantId), Times.Once);
    }

    [Fact]
    public async Task MisTurnos_MaterializaElMesActualYElSiguiente_YPideDesdeElMesPasado()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var proximo = hoy.AddMonths(1);
        var inicioHistorial = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(-1);

        await _service.MisTurnosAsync(UserId);

        _turnoService.Verify(s => s.GenerarTurnosDelMesAsync(hoy.Year, hoy.Month, It.IsAny<CancellationToken>()), Times.Once);
        _turnoService.Verify(s => s.GenerarTurnosDelMesAsync(proximo.Year, proximo.Month, It.IsAny<CancellationToken>()), Times.Once);
        _turnos.Verify(t => t.ListarPorAlumnoEntreAsync(_ficha.Id, inicioHistorial, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private Turno TurnoDelGrupo(DateOnly fecha, bool miPresente = true)
    {
        var turno = new Turno
        {
            HorarioId = Guid.NewGuid(),
            Fecha = fecha,
            HoraInicio = new TimeOnly(18, 0),
            DuracionMinutos = 60,
            Horario = new Horario
            {
                CanchaId = Guid.NewGuid(),
                GrupoId = Guid.NewGuid(),
                Grupo = new Grupo { Nombre = "Intermedios", Categoria = CategoriaAlumno.Cuarta },
                Dia = DayOfWeek.Tuesday,
                HoraInicio = new TimeOnly(18, 0),
                DuracionMinutos = 60,
            },
        };
        turno.Participantes.Add(new TurnoParticipante { Turno = turno, AlumnoId = _ficha.Id, Presente = miPresente });
        turno.Participantes.Add(new TurnoParticipante
        {
            Turno = turno,
            AlumnoId = OtroAlumnoId,
            Presente = true,
            Alumno = new Alumno
            {
                TenantId = _ficha.TenantId, Nombre = "Mateo", Apellido = "Gómez",
                Dni = "40222333", Telefono = "+549116666",
            },
        });
        return turno;
    }

    [Fact]
    public async Task MisTurnos_SeparaProximosDeHistorial_YMapeaCategoriaCompanerosYMiAsistencia()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var futuro = TurnoDelGrupo(hoy.AddDays(2));
        var pasado = TurnoDelGrupo(hoy.AddDays(-3), miPresente: false); // faltó
        _turnos.Setup(t => t.ListarPorAlumnoEntreAsync(_ficha.Id, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([futuro, pasado]);

        var mis = await _service.MisTurnosAsync(UserId);

        var proximo = Assert.Single(mis.Proximos);
        Assert.Equal("Intermedios", proximo.Titulo);
        Assert.Equal("Cuarta", proximo.Categoria);
        Assert.Equal(["Mateo Gómez"], proximo.Companeros); // los demás, no yo

        var historico = Assert.Single(mis.Historial);
        Assert.False(historico.Presente); // MI asistencia, no la del compañero
    }

    [Fact]
    public async Task MisTurnos_ElTurnoDeHoy_EsProximo_NoHistorial()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        _turnos.Setup(t => t.ListarPorAlumnoEntreAsync(_ficha.Id, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([TurnoDelGrupo(hoy)]);

        var mis = await _service.MisTurnosAsync(UserId);

        Assert.Single(mis.Proximos);
        Assert.Empty(mis.Historial);
    }

    // ─────────────────────────────────────────────
    // Cancelar MI turno (aviso individual: el turno sigue, mi cargo queda)
    // ─────────────────────────────────────────────

    private Turno TurnoConmigo(DateOnly fecha, TimeOnly? hora = null)
    {
        var turno = TurnoDelGrupo(fecha);
        turno.HoraInicio = hora ?? new TimeOnly(23, 59); // futuro dentro del día
        _turnos.Setup(t => t.ObtenerAsync(turno.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(turno);
        return turno;
    }

    [Fact]
    public async Task CancelarMiTurno_RegistraElAvisoEnMiParticipacion_SinTocarElTurno()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var turno = TurnoConmigo(hoy.AddDays(2));

        await _service.CancelarMiTurnoAsync(UserId, turno.Id, "Viaje de trabajo");

        var mia = turno.Participantes.Single(p => p.AlumnoId == _ficha.Id);
        Assert.NotNull(mia.CanceloEl);
        Assert.Equal("Viaje de trabajo", mia.CancelacionMotivo);
        Assert.False(mia.Presente); // avisó que no viene
        // El turno SIGUE para los demás y nadie toca la plata
        Assert.Equal(EstadoTurno.Programado, turno.Estado);
        var otro = turno.Participantes.Single(p => p.AlumnoId == OtroAlumnoId);
        Assert.Null(otro.CanceloEl);
        _turnos.Verify(t => t.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelarMiTurno_DeUnTurnoDondeNoParticipo_Lanza()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var turno = TurnoConmigo(hoy.AddDays(2));
        turno.Participantes.Remove(turno.Participantes.Single(p => p.AlumnoId == _ficha.Id));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CancelarMiTurnoAsync(UserId, turno.Id, "x"));
    }

    [Fact]
    public async Task CancelarMiTurno_Inexistente_Lanza()
    {
        _turnos.Setup(t => t.ObtenerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Turno?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CancelarMiTurnoAsync(UserId, Guid.NewGuid(), "x"));
    }

    [Fact]
    public async Task CancelarMiTurno_TurnoPasado_Lanza()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var turno = TurnoConmigo(hoy.AddDays(-1));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CancelarMiTurnoAsync(UserId, turno.Id, "x"));
    }

    [Fact]
    public async Task CancelarMiTurno_HoyPeroYaEmpezo_Lanza()
    {
        var ahora = DateTime.UtcNow;
        var turno = TurnoConmigo(
            DateOnly.FromDateTime(ahora),
            TimeOnly.FromDateTime(ahora).AddMinutes(-10)); // arrancó hace 10'

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CancelarMiTurnoAsync(UserId, turno.Id, "x"));
    }

    [Fact]
    public async Task CancelarMiTurno_YaAvise_Lanza()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var turno = TurnoConmigo(hoy.AddDays(2));
        turno.Participantes.Single(p => p.AlumnoId == _ficha.Id).CanceloEl = DateTime.UtcNow;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CancelarMiTurnoAsync(UserId, turno.Id, "otra vez"));
    }

    [Fact]
    public async Task CancelarMiTurno_TurnoYaCanceladoEntero_Lanza()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var turno = TurnoConmigo(hoy.AddDays(2));
        turno.Estado = EstadoTurno.Cancelado;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CancelarMiTurnoAsync(UserId, turno.Id, "x"));
    }

    [Fact]
    public async Task CancelarMiTurno_SinMotivo_Lanza()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var turno = TurnoConmigo(hoy.AddDays(2));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CancelarMiTurnoAsync(UserId, turno.Id, "   "));
    }

    [Fact]
    public async Task MiCuota_DevuelveSoloMiLiquidacion()
    {
        _cuotas.Setup(c => c.ObtenerMesAsync(2026, 7, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new LiquidacionMesDto
               {
                   Anio = 2026,
                   Mes = 7,
                   Liquidaciones =
                   [
                       new AlumnoLiquidacionDto { AlumnoId = _ficha.Id, Nombre = "Lucas", Total = 8_000m },
                       new AlumnoLiquidacionDto { AlumnoId = OtroAlumnoId, Nombre = "Sofía", Total = 4_000m },
                   ],
               });

        var mia = await _service.MiCuotaAsync(UserId, 2026, 7);

        Assert.NotNull(mia);
        Assert.Equal(_ficha.Id, mia!.AlumnoId);
        Assert.Equal(8_000m, mia.Total);
    }

    [Fact]
    public async Task MiCuota_SinMovimientosEnElMes_DevuelveNull()
    {
        _cuotas.Setup(c => c.ObtenerMesAsync(2026, 7, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new LiquidacionMesDto { Anio = 2026, Mes = 7 });

        Assert.Null(await _service.MiCuotaAsync(UserId, 2026, 7));
    }

    [Fact]
    public async Task MiPerfil_DevuelveLaFichaConElClub()
    {
        var perfil = await _service.MiPerfilAsync(UserId);

        Assert.Equal("Lucas", perfil.Nombre);
        Assert.Equal("Club Demo", perfil.Club);
    }

    // ─────────────────────────────────────────────
    // El alumno edita SUS datos de contacto (el resto es del profe)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task ActualizarMiPerfil_CambiaTelefonoYEmail_YGuarda()
    {
        var dto = new ActualizarMiPerfilDto { Telefono = "+549117777", Email = "lucas@mail.com" };

        var perfil = await _service.ActualizarMiPerfilAsync(UserId, dto);

        Assert.Equal("+549117777", _ficha.Telefono);
        Assert.Equal("lucas@mail.com", _ficha.Email);
        Assert.Equal("+549117777", perfil.Telefono);
        _alumnos.Verify(a => a.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActualizarMiPerfil_SinTelefono_Lanza()
    {
        // El teléfono es el contacto mínimo de la ficha: no puede quedar vacío
        var dto = new ActualizarMiPerfilDto { Telefono = "  ", Email = null };

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.ActualizarMiPerfilAsync(UserId, dto));

        _alumnos.Verify(a => a.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ActualizarMiPerfil_SinFichaVinculada_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.ActualizarMiPerfilAsync(Guid.NewGuid(), new ActualizarMiPerfilDto { Telefono = "x" }));
    }

    [Fact]
    public async Task ActualizarMiPerfil_CambiaLaCategoria_EnLaFicha()
    {
        // Un solo campo en la ficha = se refleja en todos lados (M3, "por ahora")
        _ficha.Categoria = CategoriaAlumno.Cuarta;
        var dto = new ActualizarMiPerfilDto { Telefono = "+549117777", Categoria = CategoriaAlumno.Tercera };

        var perfil = await _service.ActualizarMiPerfilAsync(UserId, dto);

        Assert.Equal(CategoriaAlumno.Tercera, _ficha.Categoria);
        Assert.Equal("Tercera", perfil.Categoria);
    }

    [Fact]
    public async Task ActualizarFoto_GuardaLaImagen()
    {
        await _service.ActualizarFotoAsync(UserId, "data:image/jpeg;base64,/9j/abc123");

        Assert.Equal("data:image/jpeg;base64,/9j/abc123", _ficha.FotoUrl);
        _alumnos.Verify(a => a.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActualizarFoto_Vacia_QuitaLaFoto()
    {
        _ficha.FotoUrl = "data:image/png;base64,algo";

        await _service.ActualizarFotoAsync(UserId, null);

        Assert.Null(_ficha.FotoUrl);
    }

    [Fact]
    public async Task ActualizarFoto_NoEsImagen_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.ActualizarFotoAsync(UserId, "data:text/html;base64,PHNjcmlwdD4="));

        Assert.Null(_ficha.FotoUrl);
    }

    [Fact]
    public async Task ActualizarFoto_MuyPesada_Lanza()
    {
        var enorme = "data:image/jpeg;base64," + new string('A', 700_001);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.ActualizarFotoAsync(UserId, enorme));
    }
}
