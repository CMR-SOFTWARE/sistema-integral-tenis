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
    private readonly PortalService _service;
    private readonly Alumno _ficha;

    public PortalServiceTests()
    {
        _alumnos = new Mock<IAlumnoRepository>();
        _turnos = new Mock<ITurnoRepository>();
        _turnoService = new Mock<ITurnoService>();
        _cuotas = new Mock<ICuotaService>();
        _service = new PortalService(_alumnos.Object, _turnos.Object, _turnoService.Object, _cuotas.Object);

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
}
