using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Admin de plataforma (TDD): arma las métricas globales y gestiona el estado
/// de los clubes. Regla: solo se puede pasar a Activo o Suspendido, y el club
/// tiene que existir.
/// </summary>
public class AdminServiceTests
{
    private readonly Mock<IAdminRepository> _repo;
    private readonly Mock<ICredencialesService> _credenciales;
    private readonly Mock<IMembresiaTenantRepository> _membresias;
    private readonly Mock<IAuthService> _auth;
    private readonly AdminService _service;

    public AdminServiceTests()
    {
        _repo = new Mock<IAdminRepository>();
        _credenciales = new Mock<ICredencialesService>();
        _membresias = new Mock<IMembresiaTenantRepository>();
        _auth = new Mock<IAuthService>();
        _service = new AdminService(_repo.Object, _credenciales.Object, _membresias.Object, _auth.Object);

        // Por defecto: el teléfono está libre
        _credenciales.Setup(c => c.BuscarTitularPorTelefonoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((TitularInfo?)null);

        // Defaults (0 en todo)
        _repo.Setup(r => r.ContarClubesPorEstadoAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Dictionary<EstadoTenant, int>());
        _repo.Setup(r => r.ContarStaffActivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _repo.Setup(r => r.ContarUsuariosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _repo.Setup(r => r.IngresosDelMesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(0m);
        _repo.Setup(r => r.ContarClubesNuevosAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _repo.Setup(r => r.ContarAlumnosNuevosAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
    }

    [Fact]
    public async Task Metricas_ArmaLosTotalesYSumaProfes()
    {
        _repo.Setup(r => r.ContarClubesPorEstadoAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Dictionary<EstadoTenant, int>
             {
                 [EstadoTenant.Activo] = 5,
                 [EstadoTenant.PendientePago] = 2,
                 [EstadoTenant.Suspendido] = 1,
             });
        _repo.Setup(r => r.ContarStaffActivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _repo.Setup(r => r.ContarUsuariosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(120);
        _repo.Setup(r => r.IngresosDelMesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(450_000m);

        var m = await _service.MetricasAsync();

        Assert.Equal(8, m.TotalClubes); // 5+2+1
        Assert.Equal(5, m.ClubesActivos);
        Assert.Equal(2, m.ClubesPendientes);
        Assert.Equal(1, m.ClubesSuspendidos);
        Assert.Equal(11, m.TotalProfes); // 8 dueños + 3 staff
        Assert.Equal(120, m.TotalUsuarios);
        Assert.Equal(450_000m, m.IngresosMes);
    }

    [Fact]
    public async Task CambiarEstado_Suspende_UnClub()
    {
        var club = new Tenant { Subdominio = "x", Nombre = "X", Estado = EstadoTenant.Activo };
        _repo.Setup(r => r.ObtenerTenantAsync(club.Id, It.IsAny<CancellationToken>())).ReturnsAsync(club);

        await _service.CambiarEstadoClubAsync(club.Id, EstadoTenant.Suspendido);

        Assert.Equal(EstadoTenant.Suspendido, club.Estado);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CambiarEstado_ClubInexistente_Lanza()
    {
        _repo.Setup(r => r.ObtenerTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Tenant?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CambiarEstadoClubAsync(Guid.NewGuid(), EstadoTenant.Suspendido));
    }

    [Fact]
    public async Task CambiarEstado_APendientePago_Lanza()
    {
        var club = new Tenant { Subdominio = "x", Nombre = "X", Estado = EstadoTenant.Activo };
        _repo.Setup(r => r.ObtenerTenantAsync(club.Id, It.IsAny<CancellationToken>())).ReturnsAsync(club);

        // Solo se permite Activo o Suspendido (PendientePago es el estado inicial, no se fuerza)
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CambiarEstadoClubAsync(club.Id, EstadoTenant.PendientePago));
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Alta de academia desde Plataforma (Bloque 6, pedido 10) ──

    private static AltaClubDto AltaDto() => new()
    {
        NombreClub = "Academia Nueva", Nombre = "Marta", Apellido = "Díaz", Telefono = "1133445566",
    };

    [Fact]
    public async Task CrearClub_CasoFeliz_SaltaElCheckoutYNaceActiva()
    {
        var userId = Guid.NewGuid();
        var usuario = new Usuario { Id = userId, Nombre = "Marta", Apellido = "Díaz" };
        var tenant = new Tenant { Subdominio = "academia-nueva", Nombre = "Academia Nueva", Estado = EstadoTenant.PendientePago, OwnerUserId = userId };

        _credenciales.Setup(c => c.CrearConTemporalAsync(
                "1133445566", "Marta", "Díaz", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CredencialesCreadas(userId, "1133445566"));
        _membresias.Setup(m => m.ObtenerUsuarioAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _auth.Setup(a => a.CrearTenantParaAsync(usuario, "Academia Nueva", It.IsAny<CancellationToken>()))
             .ReturnsAsync(tenant);

        var res = await _service.CrearClubAsync(AltaDto());

        // La activación la hace AuthService (misma costura que el webhook de MP real)
        _auth.Verify(a => a.ActivarTenantAsync(usuario, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("Activo", res.Club.Estado);
        Assert.Equal("Marta Díaz", res.Club.Profesor);
        Assert.Equal("1133445566", res.PasswordTemporal);
    }

    [Fact]
    public async Task CrearClub_TelefonoYaTieneCuenta_Lanza()
    {
        _credenciales.Setup(c => c.BuscarTitularPorTelefonoAsync("1133445566", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new TitularInfo(Guid.NewGuid(), "Otra", "Persona"));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearClubAsync(AltaDto()));
        _credenciales.Verify(c => c.CrearConTemporalAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
