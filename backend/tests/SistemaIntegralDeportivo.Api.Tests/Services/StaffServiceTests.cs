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
    private readonly Mock<IUsuarioActual> _usuario;
    private readonly StaffService _service;

    public StaffServiceTests()
    {
        _repo = new Mock<IMembresiaTenantRepository>();
        _tenants = new Mock<ITenantRepository>();
        _credenciales = new Mock<ICredencialesService>();
        _usuario = new Mock<IUsuarioActual>();
        _service = new StaffService(_repo.Object, _tenants.Object, _credenciales.Object, _usuario.Object);

        _tenants.Setup(t => t.ObtenerActualAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Tenant { Subdominio = "d", Nombre = "Academia", OwnerUserId = OwnerId });
        // Por defecto: no hay cuenta con ese email
        _repo.Setup(r => r.BuscarUsuarioPorTelefonoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
        // El teléfono es la llave; el email va como dato opcional
        _credenciales.Setup(c => c.CrearConTemporalAsync(
                "1122334455", "Ana", "Gómez", null, "ana@mail.com", It.IsAny<CancellationToken>()))
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
        _repo.Setup(r => r.BuscarUsuarioPorTelefonoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
        _repo.Setup(r => r.BuscarUsuarioPorTelefonoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
        _repo.Setup(r => r.BuscarUsuarioPorTelefonoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(u);
        _repo.Setup(r => r.ObtenerPorUserIdAsync(u.Id, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new MembresiaTenant { UserId = u.Id, Activo = true });

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.AgregarAsync(Dto()));
    }

    [Fact]
    public async Task Agregar_ExStaffInactivo_LoReactiva_SinRecrear()
    {
        var u = Usuario(Guid.NewGuid());
        _repo.Setup(r => r.BuscarUsuarioPorTelefonoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
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

    // ── Borrado REAL del profe empleado (el dueño se equivocó al cargarlo) ──

    [Fact]
    public async Task EliminarDefinitivo_SinOtroRol_BorraMembresiaYLogin()
    {
        var uid = Guid.NewGuid();
        var m = new MembresiaTenant { UserId = uid, Activo = true };
        _repo.Setup(r => r.ObtenerAsync(m.Id, It.IsAny<CancellationToken>())).ReturnsAsync(m);
        _repo.Setup(r => r.TieneOtrosRolesAsync(uid, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _service.EliminarDefinitivoAsync(m.Id);

        _repo.Verify(r => r.EliminarConReferenciasAsync(m, It.IsAny<CancellationToken>()), Times.Once);
        _credenciales.Verify(c => c.EliminarAsync(uid, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EliminarDefinitivo_TambienEsAlumnoODueño_BorraMembresiaPeroConservaLogin()
    {
        var uid = Guid.NewGuid();
        var m = new MembresiaTenant { UserId = uid, Activo = true };
        _repo.Setup(r => r.ObtenerAsync(m.Id, It.IsAny<CancellationToken>())).ReturnsAsync(m);
        _repo.Setup(r => r.TieneOtrosRolesAsync(uid, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _service.EliminarDefinitivoAsync(m.Id);

        _repo.Verify(r => r.EliminarConReferenciasAsync(m, It.IsAny<CancellationToken>()), Times.Once);
        _credenciales.Verify(c => c.EliminarAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EliminarDefinitivo_Inexistente_Lanza()
    {
        _repo.Setup(r => r.ObtenerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((MembresiaTenant?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.EliminarDefinitivoAsync(Guid.NewGuid()));
        _repo.Verify(r => r.EliminarConReferenciasAsync(It.IsAny<MembresiaTenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Profes asignables (dueño + staff activos) ──

    [Fact]
    public async Task ListarAsignables_IncluyeAlDueñoYSoloStaffActivos()
    {
        _repo.Setup(r => r.ObtenerUsuarioAsync(OwnerId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Usuario { Id = OwnerId, Nombre = "Juan", Apellido = "Head" });
        var activo = Usuario(Guid.NewGuid());
        var inactivoU = Usuario(Guid.NewGuid());
        _repo.Setup(r => r.ListarConUsuarioAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(new[]
             {
                 (new MembresiaTenant { UserId = activo.Id, Activo = true }, activo),
                 (new MembresiaTenant { UserId = inactivoU.Id, Activo = false }, inactivoU),
             });

        var res = await _service.ListarAsignablesAsync();

        Assert.Equal(2, res.Count); // dueño + 1 staff activo (el inactivo no)
        Assert.Contains(res, p => p.UserId == OwnerId && p.EsDueño);
        Assert.Contains(res, p => p.UserId == activo.Id && !p.EsDueño);
        Assert.DoesNotContain(res, p => p.UserId == inactivoU.Id);
    }

    [Fact]
    public async Task ListarAsignables_DirectorNoDaClases_NoIncluyeAlDueño()
    {
        // El dueño (Director) marcó que NO da clases → no se ofrece como profe.
        _tenants.Setup(t => t.ObtenerActualAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Tenant { Subdominio = "d", Nombre = "Academia", OwnerUserId = OwnerId, DirectorDaClases = false });
        _repo.Setup(r => r.ObtenerUsuarioAsync(OwnerId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Usuario { Id = OwnerId, Nombre = "Juan", Apellido = "Head" });
        var activo = Usuario(Guid.NewGuid());
        _repo.Setup(r => r.ListarConUsuarioAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(new[] { (new MembresiaTenant { UserId = activo.Id, Activo = true }, activo) });

        var res = await _service.ListarAsignablesAsync();

        Assert.Single(res); // solo el staff activo, NO el dueño
        Assert.DoesNotContain(res, p => p.EsDueño);
        Assert.Contains(res, p => p.UserId == activo.Id);
    }

    [Fact]
    public async Task Desvincularme_DesactivaMiMembresia()
    {
        var yo = Guid.NewGuid();
        _usuario.Setup(u => u.UserId).Returns(yo);
        var mia = new MembresiaTenant { UserId = yo, Activo = true };
        _repo.Setup(r => r.ObtenerPorUserIdAsync(yo, It.IsAny<CancellationToken>())).ReturnsAsync(mia);

        await _service.DesvincularmeAsync();

        Assert.False(mia.Activo);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Desvincularme_SinMembresia_Lanza()
    {
        _usuario.Setup(u => u.UserId).Returns(Guid.NewGuid());
        _repo.Setup(r => r.ObtenerPorUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((MembresiaTenant?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.DesvincularmeAsync());
    }

    [Fact]
    public async Task EsAsignable_DueñoYStaffActivo_True_OtroFalse()
    {
        var activo = Guid.NewGuid();
        _repo.Setup(r => r.ObtenerPorUserIdAsync(activo, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new MembresiaTenant { UserId = activo, Activo = true });
        var inactivo = Guid.NewGuid();
        _repo.Setup(r => r.ObtenerPorUserIdAsync(inactivo, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new MembresiaTenant { UserId = inactivo, Activo = false });

        Assert.True(await _service.EsAsignableAsync(OwnerId));
        Assert.True(await _service.EsAsignableAsync(activo));
        Assert.False(await _service.EsAsignableAsync(inactivo));
        Assert.False(await _service.EsAsignableAsync(Guid.NewGuid())); // desconocido
    }

    // ── Editar la ficha del empleado (datos del Usuario + valor hora; el celular no) ──

    [Fact]
    public async Task Editar_ActualizaDatosDelUsuarioYValorHora()
    {
        var m = new MembresiaTenant { UserId = Guid.NewGuid(), ValorHora = 5_000m };
        var u = Usuario(m.UserId);
        _repo.Setup(r => r.ObtenerAsync(m.Id, It.IsAny<CancellationToken>())).ReturnsAsync(m);
        _repo.Setup(r => r.ObtenerUsuarioAsync(m.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(u);
        var dto = new UpdateStaffDto
        {
            Nombre = "Ana María", Apellido = "López", Email = "nueva@mail.com",
            Dni = "30111222", FechaNacimiento = new DateTime(1990, 5, 1), ValorHora = 8_000m,
        };

        await _service.EditarAsync(m.Id, dto);

        Assert.Equal("Ana María", u.Nombre);
        Assert.Equal("López", u.Apellido);
        Assert.Equal("nueva@mail.com", u.Email);
        Assert.Equal("30111222", u.Dni);
        Assert.Equal(new DateTime(1990, 5, 1), u.FechaNacimiento);
        Assert.Equal(8_000m, m.ValorHora);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Editar_EmailVacio_QuedaNull()
    {
        var m = new MembresiaTenant { UserId = Guid.NewGuid() };
        var u = Usuario(m.UserId, "viejo@mail.com");
        _repo.Setup(r => r.ObtenerAsync(m.Id, It.IsAny<CancellationToken>())).ReturnsAsync(m);
        _repo.Setup(r => r.ObtenerUsuarioAsync(m.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(u);

        await _service.EditarAsync(m.Id, new UpdateStaffDto { Nombre = "Ana", Apellido = "Gómez", Email = "" });

        Assert.Null(u.Email);
    }

    [Fact]
    public async Task Editar_MembresiaInexistente_Lanza()
    {
        _repo.Setup(r => r.ObtenerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((MembresiaTenant?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.EditarAsync(Guid.NewGuid(), new UpdateStaffDto { Nombre = "A", Apellido = "B" }));
    }

    [Fact]
    public async Task Editar_ValorHoraNegativo_Lanza()
    {
        var m = new MembresiaTenant { UserId = Guid.NewGuid() };
        _repo.Setup(r => r.ObtenerAsync(m.Id, It.IsAny<CancellationToken>())).ReturnsAsync(m);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.EditarAsync(m.Id, new UpdateStaffDto { Nombre = "A", Apellido = "B", ValorHora = -1m }));
    }
}
