using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Alta/baja de profes empleados (Staff), TDD. El DUEÑO le crea la cuenta al profe
/// (como a un alumno): cuenta dedicada + clave temporal. Reglas: no al propio dueño;
/// si el email ya tiene cuenta pero nunca fue staff acá, no se pisa; si fue staff y
/// quedó inactivo, se reactiva. El profe queda con rol Staff.
/// </summary>
public class StaffServiceTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    private readonly Mock<IMembresiaTenantRepository> _repo;
    private readonly Mock<ITenantRepository> _tenants;
    private readonly Mock<ICredencialesService> _credenciales;
    private readonly StaffService _service;

    public StaffServiceTests()
    {
        _repo = new Mock<IMembresiaTenantRepository>();
        _tenants = new Mock<ITenantRepository>();
        _credenciales = new Mock<ICredencialesService>();
        _service = new StaffService(_repo.Object, _tenants.Object, _credenciales.Object);

        _tenants.Setup(t => t.ObtenerActualAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Tenant { Subdominio = "d", Nombre = "Academia", OwnerUserId = OwnerId });
        // Por defecto: no hay cuenta con ese email
        _repo.Setup(r => r.BuscarUsuarioPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Usuario?)null);
    }

    private static AgregarStaffDto Dto(string email = "ana@mail.com") => new()
    {
        Nombre = "Ana", Apellido = "Gómez", Email = email, Telefono = "1122334455",
    };

    private static Usuario Usuario(Guid id, string email = "ana@mail.com") => new()
    {
        Id = id, Nombre = "Ana", Apellido = "Gómez", Email = email,
    };

    [Fact]
    public async Task Agregar_CasoFeliz_CreaLaCuentaYLoSumaComoStaff()
    {
        var uid = Guid.NewGuid();
        _credenciales.Setup(c => c.CrearConTemporalAsync(
                "ana@mail.com", "Ana", "Gómez", null, "1122334455", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CredencialesCreadas(uid, "1122334455"));
        MembresiaTenant? creada = null;
        _repo.Setup(r => r.AgregarAsync(It.IsAny<MembresiaTenant>(), It.IsAny<CancellationToken>()))
             .Callback((MembresiaTenant m, CancellationToken _) => creada = m).Returns(Task.CompletedTask);

        var res = await _service.AgregarAsync(Dto());

        Assert.NotNull(creada);
        Assert.Equal(uid, creada!.UserId);
        Assert.Equal(RolTenant.Staff, creada.Rol);
        Assert.True(creada.Activo);
        Assert.Equal("1122334455", res.PasswordTemporal); // se muestra una vez
        Assert.Equal("Ana", res.Staff.Nombre);
        Assert.Equal(uid, res.Staff.UserId);
    }

    [Fact]
    public async Task Agregar_EsElDueño_Lanza()
    {
        _repo.Setup(r => r.BuscarUsuarioPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(Usuario(OwnerId));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.AgregarAsync(Dto("dueño@mail.com")));
        _credenciales.Verify(c => c.CrearConTemporalAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Agregar_EmailYaEnUsoPeroNuncaFueStaff_Lanza()
    {
        var otro = Usuario(Guid.NewGuid());
        _repo.Setup(r => r.BuscarUsuarioPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(otro);
        _repo.Setup(r => r.ObtenerPorUserIdAsync(otro.Id, It.IsAny<CancellationToken>()))
             .ReturnsAsync((MembresiaTenant?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.AgregarAsync(Dto()));
        _credenciales.Verify(c => c.CrearConTemporalAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Agregar_YaEsMiembroActivo_Lanza()
    {
        var u = Usuario(Guid.NewGuid());
        _repo.Setup(r => r.BuscarUsuarioPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(u);
        _repo.Setup(r => r.ObtenerPorUserIdAsync(u.Id, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new MembresiaTenant { UserId = u.Id, Activo = true });

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.AgregarAsync(Dto()));
    }

    [Fact]
    public async Task Agregar_ExStaffInactivo_LoReactiva_SinRecrear()
    {
        var u = Usuario(Guid.NewGuid());
        _repo.Setup(r => r.BuscarUsuarioPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(u);
        var vieja = new MembresiaTenant { UserId = u.Id, Activo = false };
        _repo.Setup(r => r.ObtenerPorUserIdAsync(u.Id, It.IsAny<CancellationToken>())).ReturnsAsync(vieja);

        var res = await _service.AgregarAsync(Dto());

        Assert.True(vieja.Activo);
        Assert.Null(res.PasswordTemporal); // ya tenía cuenta: no se genera clave nueva
        _credenciales.Verify(c => c.CrearConTemporalAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.AgregarAsync(It.IsAny<MembresiaTenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CambiarActivo_Desactiva()
    {
        var m = new MembresiaTenant { UserId = Guid.NewGuid(), Activo = true };
        _repo.Setup(r => r.ObtenerAsync(m.Id, It.IsAny<CancellationToken>())).ReturnsAsync(m);

        await _service.CambiarActivoAsync(m.Id, false);

        Assert.False(m.Activo);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CambiarActivo_Inexistente_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CambiarActivoAsync(Guid.NewGuid(), false));
    }
}
