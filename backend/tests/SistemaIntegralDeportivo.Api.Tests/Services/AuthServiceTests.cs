using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Reglas de identidad y membresías (TDD, ADR-0007): la sesión refleja las
/// membresías reales, y el reclamo de ficha solo procede si la ficha está
/// libre Y coincide con el DNI/teléfono del usuario.
/// </summary>
public class AuthServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<IAlumnoRepository> _alumnos;
    private readonly Mock<ITenantRepository> _tenants;
    private readonly Mock<ITokenService> _tokens;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _alumnos = new Mock<IAlumnoRepository>();
        _tenants = new Mock<ITenantRepository>();
        _tokens = new Mock<ITokenService>();
        _service = new AuthService(_alumnos.Object, _tenants.Object, _tokens.Object);

        // Por defecto: no es dueño de tenants, sin ficha vinculada ni coincidencias
        _tenants.Setup(t => t.ObtenerPorOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tenant?)null);
        _alumnos.Setup(a => a.ObtenerPorUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Alumno?)null);
        _alumnos.Setup(a => a.BuscarReclamablesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
        _tokens.Setup(t => t.Generar(It.IsAny<Usuario>(), It.IsAny<Tenant?>()))
               .Returns("jwt-de-prueba");
    }

    private static Usuario Jugador(string? dni = "30111222", string? telefono = "+549115555") => new()
    {
        Id = UserId,
        UserName = "lucas@mail.com",
        Email = "lucas@mail.com",
        Nombre = "Lucas",
        Apellido = "Calderón",
        Dni = dni,
        PhoneNumber = telefono,
    };

    private static Alumno Ficha(Guid? userId = null) => new()
    {
        TenantId = Guid.NewGuid(),
        Tenant = new Tenant { Subdominio = "demo", Nombre = "Club Demo" },
        Nombre = "Lucas",
        Apellido = "Calderón",
        Dni = "30111222",
        Telefono = "+549115555",
        UserId = userId,
    };

    // ─────────────────────────────────────────────
    // Sesión: refleja las membresías reales
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Sesion_DuenioDeTenant_EsProfesor_YElTokenLlevaSuTenant()
    {
        var profe = Jugador();
        var suClub = new Tenant { Subdominio = "mi-club", Nombre = "Mi Club", OwnerUserId = UserId };
        _tenants.Setup(t => t.ObtenerPorOwnerAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(suClub);

        var sesion = await _service.ArmarSesionAsync(profe, incluirToken: true);

        Assert.True(sesion.EsProfesor);
        Assert.Equal("jwt-de-prueba", sesion.Token);
        _tokens.Verify(t => t.Generar(profe, suClub), Times.Once); // el claim tenant sale de acá
    }

    [Fact]
    public async Task Sesion_Jugador_ElTokenVaSinTenant()
    {
        var sesion = await _service.ArmarSesionAsync(Jugador(), incluirToken: true);

        Assert.False(sesion.EsProfesor);
        _tokens.Verify(t => t.Generar(It.IsAny<Usuario>(), null), Times.Once);
    }

    [Fact]
    public async Task Sesion_SinToken_CuandoNoSePide()
    {
        var sesion = await _service.ArmarSesionAsync(Jugador(), incluirToken: false);

        Assert.Null(sesion.Token);
        _tokens.Verify(t => t.Generar(It.IsAny<Usuario>(), It.IsAny<Tenant?>()), Times.Never);
    }

    [Fact]
    public async Task Sesion_SinFichaVinculada_OfreceLasCoincidencias()
    {
        var ficha = Ficha();
        _alumnos.Setup(a => a.BuscarReclamablesAsync("30111222", "+549115555", It.IsAny<CancellationToken>()))
                .ReturnsAsync([ficha]);

        var sesion = await _service.ArmarSesionAsync(Jugador(), incluirToken: false);

        Assert.Null(sesion.Alumno);
        var oferta = Assert.Single(sesion.FichasPorReclamar);
        Assert.Equal(ficha.Id, oferta.AlumnoId);
        Assert.Equal("Club Demo", oferta.Club);
    }

    [Fact]
    public async Task Sesion_ConFichaVinculada_LaInforma_YNoOfreceReclamos()
    {
        var vinculada = Ficha(userId: UserId);
        _alumnos.Setup(a => a.ObtenerPorUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(vinculada);

        var sesion = await _service.ArmarSesionAsync(Jugador(), incluirToken: false);

        Assert.NotNull(sesion.Alumno);
        Assert.Equal(vinculada.Id, sesion.Alumno!.AlumnoId);
        Assert.Empty(sesion.FichasPorReclamar);
        _alumnos.Verify(a => a.BuscarReclamablesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Sesion_UsuarioSinDniNiTelefono_NoBuscaCoincidencias()
    {
        await _service.ArmarSesionAsync(Jugador(dni: null, telefono: null), incluirToken: false);

        _alumnos.Verify(a => a.BuscarReclamablesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─────────────────────────────────────────────
    // Reclamo de ficha: solo si está libre Y coincide conmigo
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Reclamar_FichaCoincidente_VinculaElUsuario()
    {
        var ficha = Ficha();
        _alumnos.Setup(a => a.BuscarReclamablesAsync("30111222", "+549115555", It.IsAny<CancellationToken>()))
                .ReturnsAsync([ficha]);

        await _service.ReclamarFichaAsync(Jugador(), ficha.Id);

        Assert.Equal(UserId, ficha.UserId);
        _alumnos.Verify(a => a.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reclamar_FichaQueNoEstaEntreMisCoincidencias_Lanza()
    {
        // La ficha existe pero es de OTRA persona (o ya fue reclamada):
        // no aparece entre las candidatas del usuario
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.ReclamarFichaAsync(Jugador(), Guid.NewGuid()));

        _alumnos.Verify(a => a.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reclamar_SinDniNiTelefonoCargados_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.ReclamarFichaAsync(Jugador(dni: null, telefono: null), Guid.NewGuid()));
    }
}
