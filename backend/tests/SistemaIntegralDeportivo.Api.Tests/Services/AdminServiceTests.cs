using Moq;
using SistemaIntegralDeportivo.Api.Common;
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
    private readonly AdminService _service;

    public AdminServiceTests()
    {
        _repo = new Mock<IAdminRepository>();
        _service = new AdminService(_repo.Object);

        // Defaults (0 en todo)
        _repo.Setup(r => r.ContarClubesPorEstadoAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Dictionary<EstadoTenant, int>());
        _repo.Setup(r => r.ContarStaffActivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _repo.Setup(r => r.ContarAlumnosActivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
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
        _repo.Setup(r => r.ContarAlumnosActivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(120);
        _repo.Setup(r => r.IngresosDelMesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(450_000m);

        var m = await _service.MetricasAsync();

        Assert.Equal(8, m.TotalClubes); // 5+2+1
        Assert.Equal(5, m.ClubesActivos);
        Assert.Equal(2, m.ClubesPendientes);
        Assert.Equal(1, m.ClubesSuspendidos);
        Assert.Equal(11, m.TotalProfes); // 8 dueños + 3 staff
        Assert.Equal(120, m.TotalAlumnos);
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
}
