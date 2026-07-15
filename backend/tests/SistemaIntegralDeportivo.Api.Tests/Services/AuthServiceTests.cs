using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Reglas de identidad y membresías (TDD, ADR-0007 + plan v2): la sesión
/// refleja las membresías reales, el registro de profe crea su club en
/// PENDIENTE_PAGO y la activación es idempotente.
/// </summary>
public class AuthServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<IAlumnoRepository> _alumnos;
    private readonly Mock<ITenantRepository> _tenants;
    private readonly Mock<ITokenService> _tokens;
    private readonly Mock<ISedeRepository> _sedes;
    private readonly Mock<ITenantActual> _tenantActual;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _alumnos = new Mock<IAlumnoRepository>();
        _tenants = new Mock<ITenantRepository>();
        _tokens = new Mock<ITokenService>();
        _sedes = new Mock<ISedeRepository>();
        _tenantActual = new Mock<ITenantActual>();
        _service = new AuthService(
            _alumnos.Object, _tenants.Object, _tokens.Object, _sedes.Object, _tenantActual.Object);
        _sedes.Setup(s => s.AgregarAsync(It.IsAny<Sede>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((Sede s, CancellationToken _) => s);

        // Por defecto: no es dueño de tenants y sin ficha vinculada
        _tenants.Setup(t => t.ObtenerPorOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tenant?)null);
        _alumnos.Setup(a => a.ObtenerPorUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Alumno?)null);
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
    public async Task Sesion_DuenioDeTenantActivo_EsProfesor_YElTokenLlevaSuTenant()
    {
        var profe = Jugador();
        var suClub = new Tenant
        {
            Subdominio = "mi-club", Nombre = "Mi Club",
            OwnerUserId = UserId, Estado = EstadoTenant.Activo,
        };
        _tenants.Setup(t => t.ObtenerPorOwnerAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(suClub);

        var sesion = await _service.ArmarSesionAsync(profe, incluirToken: true);

        Assert.True(sesion.EsProfesor);
        Assert.Equal("Activo", sesion.EstadoTenant);
        Assert.Equal("jwt-de-prueba", sesion.Token);
        _tokens.Verify(t => t.Generar(profe, suClub), Times.Once); // el claim tenant sale de acá
    }

    [Fact]
    public async Task Sesion_DuenioPendienteDePago_NoEsProfesor_YExponeElEstado()
    {
        // Un profe que no pagó todavía NO pasa la policy: el token va SIN tenant
        var profe = Jugador();
        var suClub = new Tenant
        {
            Subdominio = "mi-club", Nombre = "Mi Club",
            OwnerUserId = UserId, Estado = EstadoTenant.PendientePago,
        };
        _tenants.Setup(t => t.ObtenerPorOwnerAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(suClub);

        var sesion = await _service.ArmarSesionAsync(profe, incluirToken: true);

        Assert.False(sesion.EsProfesor);
        Assert.Equal("PendientePago", sesion.EstadoTenant);
        _tokens.Verify(t => t.Generar(profe, null), Times.Once);
    }

    // ─────────────────────────────────────────────
    // Registro de profesor: su tenant nace PENDIENTE_PAGO
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CrearTenant_NacePendienteConOwnerYSubdominioSlug()
    {
        Tenant? creado = null;
        _tenants.Setup(t => t.AgregarAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
                .Callback((Tenant t, CancellationToken _) => creado = t)
                .Returns(Task.CompletedTask);

        await _service.CrearTenantParaAsync(Jugador(), "Academia Río Cuarto");

        Assert.NotNull(creado);
        Assert.Equal("academia-rio-cuarto", creado!.Subdominio); // minúsculas, sin acentos
        Assert.Equal("Academia Río Cuarto", creado.Nombre);
        Assert.Equal(EstadoTenant.PendientePago, creado.Estado);
        Assert.Equal(UserId, creado.OwnerUserId);
        Assert.Equal(TipoTenant.Profesor, creado.Tipo);
        _tenants.Verify(t => t.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CrearTenant_CreaLaPrimeraSedeConElNombreDelClub()
    {
        // El caso típico (un solo lugar): el club nace con su sede lista,
        // renombrable en Configuración — sin pasos duplicados post-registro
        Tenant? tenantCreado = null;
        _tenants.Setup(t => t.AgregarAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
                .Callback((Tenant t, CancellationToken _) => tenantCreado = t)
                .Returns(Task.CompletedTask);
        Sede? sedeCreada = null;
        _sedes.Setup(s => s.AgregarAsync(It.IsAny<Sede>(), It.IsAny<CancellationToken>()))
              .Callback((Sede s, CancellationToken _) => sedeCreada = s)
              .ReturnsAsync((Sede s, CancellationToken _) => s);

        await _service.CrearTenantParaAsync(Jugador(), "Academia Río Cuarto");

        Assert.NotNull(sedeCreada);
        Assert.Equal("Academia Río Cuarto", sedeCreada!.Nombre);
        // El repo de sedes scopea por ITenantActual: hay que fijar el club nuevo ANTES
        _tenantActual.Verify(t => t.Establecer(tenantCreado!.Id), Times.Once);
    }

    [Fact]
    public async Task CrearTenant_SubdominioOcupado_AgregaSufijo()
    {
        _tenants.Setup(t => t.ExisteSubdominioAsync("club-x", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        Tenant? creado = null;
        _tenants.Setup(t => t.AgregarAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
                .Callback((Tenant t, CancellationToken _) => creado = t)
                .Returns(Task.CompletedTask);

        await _service.CrearTenantParaAsync(Jugador(), "Club X");

        Assert.Equal("club-x-2", creado!.Subdominio);
    }

    [Fact]
    public async Task CrearTenant_YaEsDuenioDeOtro_Lanza()
    {
        _tenants.Setup(t => t.ObtenerPorOwnerAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Tenant { Subdominio = "x", Nombre = "X", OwnerUserId = UserId });

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearTenantParaAsync(Jugador(), "Otro Club"));

        _tenants.Verify(t => t.AgregarAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CrearTenant_NombreVacio_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearTenantParaAsync(Jugador(), "   "));
    }

    // ─────────────────────────────────────────────
    // Activación (el "webhook" — hoy simulado, MP al desplegar)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Activar_TenantPendiente_PasaAActivo_YDevuelveTokenNuevo()
    {
        var suClub = new Tenant
        {
            Subdominio = "mi-club", Nombre = "Mi Club",
            OwnerUserId = UserId, Estado = EstadoTenant.PendientePago,
        };
        _tenants.Setup(t => t.ObtenerPorOwnerAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(suClub);

        var sesion = await _service.ActivarTenantAsync(Jugador());

        Assert.Equal(EstadoTenant.Activo, suClub.Estado);
        Assert.True(sesion.EsProfesor);
        Assert.Equal("jwt-de-prueba", sesion.Token); // token nuevo con claims profe+tenant
        _tenants.Verify(t => t.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Activar_TenantYaActivo_EsIdempotente()
    {
        // El webhook real de MP reintenta: activar dos veces no puede romper
        var suClub = new Tenant
        {
            Subdominio = "mi-club", Nombre = "Mi Club",
            OwnerUserId = UserId, Estado = EstadoTenant.Activo,
        };
        _tenants.Setup(t => t.ObtenerPorOwnerAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(suClub);

        var sesion = await _service.ActivarTenantAsync(Jugador());

        Assert.True(sesion.EsProfesor);
        _tenants.Verify(t => t.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Activar_SinTenant_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.ActivarTenantAsync(Jugador()));
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
    public async Task Sesion_ConFichaVinculada_LaInforma()
    {
        var vinculada = Ficha(userId: UserId);
        _alumnos.Setup(a => a.ObtenerPorUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(vinculada);

        var sesion = await _service.ArmarSesionAsync(Jugador(), incluirToken: false);

        Assert.NotNull(sesion.Alumno);
        Assert.Equal(vinculada.Id, sesion.Alumno!.AlumnoId);
        Assert.Equal("Club Demo", sesion.Alumno.Club);
    }

    [Fact]
    public async Task Sesion_ConTemporalSinCambiar_LoInforma()
    {
        var usuario = Jugador();
        usuario.DebeCambiarPassword = true; // lo dio de alta su profe

        var sesion = await _service.ArmarSesionAsync(usuario, incluirToken: false);

        Assert.True(sesion.DebeCambiarPassword);
    }
}
