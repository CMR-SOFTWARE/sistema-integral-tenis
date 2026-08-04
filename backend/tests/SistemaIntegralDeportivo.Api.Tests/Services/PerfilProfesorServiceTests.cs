using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// El perfil público del profe (su carta de presentación), TDD. Lo que se testea es
/// la lógica que puede lastimar: qué se acepta como imagen (los bytes, no lo que
/// declara el cliente), los topes de la galería, el reordenamiento, la limpieza de
/// los archivos viejos y quién puede ver el perfil. El almacenamiento va mockeado:
/// por eso existe la abstracción, para probar todo esto sin tocar disco ni red.
/// </summary>
public class PerfilProfesorServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<IPerfilProfesorRepository> _repo;
    private readonly Mock<IAlmacenamientoArchivos> _archivos;
    private readonly Mock<ITenantActual> _tenantActual;
    private readonly Mock<IUsuarioActual> _usuarioActual;
    private readonly Mock<ITenantRepository> _tenants;
    private readonly Mock<IMembresiaTenantRepository> _membresias;
    private readonly PerfilProfesorService _service;

    public PerfilProfesorServiceTests()
    {
        _repo = new Mock<IPerfilProfesorRepository>();
        _archivos = new Mock<IAlmacenamientoArchivos>();
        _tenantActual = new Mock<ITenantActual>();
        _usuarioActual = new Mock<IUsuarioActual>();
        _tenants = new Mock<ITenantRepository>();
        _membresias = new Mock<IMembresiaTenantRepository>();
        _service = new PerfilProfesorService(
            _repo.Object, _archivos.Object, _tenantActual.Object, _usuarioActual.Object,
            _tenants.Object, _membresias.Object, NullLogger<PerfilProfesorService>.Instance);

        _tenantActual.Setup(t => t.TenantId).Returns(TenantId);
        _usuarioActual.Setup(u => u.UserId).Returns(UserId);
        _tenants.Setup(t => t.ObtenerActualAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Tenant { Id = TenantId, Subdominio = "d", Nombre = "Academia Río Cuarto" });
        _membresias.Setup(m => m.ObtenerUsuarioAsync(UserId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Usuario { Id = UserId, Nombre = "Juan", Apellido = "Pérez" });
        _archivos.Setup(a => a.SubirAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Stream _, string __, string ruta, CancellationToken ___) => $"https://cdn/{ruta}");
    }

    // Un JPEG mínimo válido: lo que importa son los tres bytes de la firma
    private static ImagenSubida Jpeg(int bytes = 100)
    {
        var contenido = new byte[bytes];
        contenido[0] = 0xFF; contenido[1] = 0xD8; contenido[2] = 0xFF;
        return new ImagenSubida(contenido);
    }

    private PerfilProfesor PerfilExistente(PerfilProfesor? perfil = null)
    {
        perfil ??= new PerfilProfesor { TenantId = TenantId, UserId = UserId };
        _repo.Setup(r => r.ObtenerDeUsuarioAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(perfil);
        return perfil;
    }

    // ── Qué se acepta como imagen ──

    [Fact]
    public async Task SubirAvatar_ArchivoQueNoEsImagen_LoRechazaSinTocarElStorage()
    {
        PerfilExistente();
        // "%PDF" disfrazado: el navegador podría mandarlo como image/jpeg igual
        var falsa = new ImagenSubida([0x25, 0x50, 0x44, 0x46, 0x2D]);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.SubirAvatarAsync(falsa));
        _archivos.Verify(a => a.SubirAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SubirAvatar_ImagenMuyPesada_LaRechazaSinTocarElStorage()
    {
        PerfilExistente();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.SubirAvatarAsync(Jpeg(6 * 1024 * 1024)));
        _archivos.Verify(a => a.SubirAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SubirAvatar_ArchivoVacio_LoRechaza()
    {
        PerfilExistente();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.SubirAvatarAsync(new ImagenSubida([])));
    }

    [Fact]
    public async Task SubirAvatar_CasoFeliz_GuardaBajoLaCarpetaDelTenantYDelProfe()
    {
        PerfilExistente();
        string? ruta = null;
        _archivos.Setup(a => a.SubirAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .Callback((Stream _, string __, string r, CancellationToken ___) => ruta = r)
                 .ReturnsAsync("https://cdn/foto.jpg");

        var resultado = await _service.SubirAvatarAsync(Jpeg());

        Assert.Equal("https://cdn/foto.jpg", resultado.Url);
        Assert.StartsWith($"perfiles/{TenantId}/{UserId}/", ruta);
        Assert.EndsWith(".jpg", ruta);
    }

    [Fact]
    public async Task SubirAvatar_CuandoYaHabiaOtro_BorraElArchivoViejo()
    {
        PerfilExistente(new PerfilProfesor
        {
            TenantId = TenantId, UserId = UserId,
            AvatarUrl = "https://cdn/viejo.jpg", AvatarRuta = "perfiles/x/y/viejo.jpg",
        });

        await _service.SubirAvatarAsync(Jpeg());

        _archivos.Verify(a => a.EliminarAsync("perfiles/x/y/viejo.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubirAvatar_SinPerfilTodavia_LoCreaSolo()
    {
        _repo.Setup(r => r.ObtenerDeUsuarioAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync((PerfilProfesor?)null);
        PerfilProfesor? creado = null;
        _repo.Setup(r => r.AgregarAsync(It.IsAny<PerfilProfesor>(), It.IsAny<CancellationToken>()))
             .Callback((PerfilProfesor p, CancellationToken _) => creado = p).Returns(Task.CompletedTask);

        await _service.SubirAvatarAsync(Jpeg());

        Assert.NotNull(creado);
        Assert.Equal(UserId, creado!.UserId);
    }

    // ── Los textos del perfil ──

    [Fact]
    public async Task Guardar_TitularMuyLargo_LoRechaza()
    {
        PerfilExistente();
        var dto = new GuardarPerfilProfesorDto { Titular = new string('a', 81) };

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.GuardarMioAsync(dto));
    }

    [Fact]
    public async Task Guardar_BioMuyLarga_LaRechaza()
    {
        PerfilExistente();
        var dto = new GuardarPerfilProfesorDto { Bio = new string('a', 2001) };

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.GuardarMioAsync(dto));
    }

    [Fact]
    public async Task Guardar_Especialidades_LimpiaVaciasYRepetidas()
    {
        var perfil = PerfilExistente();
        var dto = new GuardarPerfilProfesorDto
        {
            Especialidades = ["  Alto rendimiento ", "Niños", "", "   ", "alto rendimiento"],
        };

        await _service.GuardarMioAsync(dto);

        Assert.Equal(["Alto rendimiento", "Niños"], perfil.Especialidades);
    }

    [Fact]
    public async Task Guardar_DemasiadasEspecialidades_LasRechaza()
    {
        PerfilExistente();
        var dto = new GuardarPerfilProfesorDto
        {
            Especialidades = [.. Enumerable.Range(1, 9).Select(i => $"Especialidad {i}")],
        };

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.GuardarMioAsync(dto));
    }

    // ── La galería ──

    [Fact]
    public async Task AgregarFoto_PasadoElTope_LaRechazaSinSubirla()
    {
        var perfil = PerfilExistente();
        for (var i = 0; i < 12; i++)
            perfil.Fotos.Add(new FotoPerfil { Url = "u", Ruta = $"r{i}", Orden = i });

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.AgregarFotoAsync(Jpeg(), null));
        _archivos.Verify(a => a.SubirAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AgregarFoto_SeVaAlFinalDeLaGaleria()
    {
        var perfil = PerfilExistente();
        perfil.Fotos.Add(new FotoPerfil { Url = "u", Ruta = "r", Orden = 0 });
        perfil.Fotos.Add(new FotoPerfil { Url = "u", Ruta = "r", Orden = 1 });

        var foto = await _service.AgregarFotoAsync(Jpeg(), "Final del provincial");

        Assert.Equal(2, foto.Orden);
        Assert.Equal("Final del provincial", foto.PieDeFoto);
    }

    [Fact]
    public async Task AgregarFoto_LaDaDeAltaPorElRepositorio()
    {
        // Sumarla solo a la colección del perfil no alcanza: como el Id lo asigna
        // C#, EF la tomaría por existente y generaría un UPDATE de cero filas.
        PerfilExistente();

        await _service.AgregarFotoAsync(Jpeg(), "Una foto");

        _repo.Verify(r => r.AgregarFoto(It.IsAny<FotoPerfil>()), Times.Once);
    }

    [Fact]
    public async Task AgregarHito_LoDaDeAltaPorElRepositorio()
    {
        PerfilExistente();

        await _service.AgregarHitoAsync(new GuardarHitoDto { Anio = 2015, Titulo = "Un hito" });

        _repo.Verify(r => r.AgregarHito(It.IsAny<HitoTrayectoria>()), Times.Once);
    }

    [Fact]
    public async Task AgregarFoto_PieDeFotoMuyLargo_LoRechaza()
    {
        PerfilExistente();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AgregarFotoAsync(Jpeg(), new string('a', 121)));
    }

    [Fact]
    public async Task EliminarFoto_BorraTambienElArchivo()
    {
        var perfil = PerfilExistente();
        var foto = new FotoPerfil { Url = "u", Ruta = "perfiles/a/b/c.jpg", Orden = 0 };
        perfil.Fotos.Add(foto);

        await _service.EliminarFotoAsync(foto.Id);

        _repo.Verify(r => r.EliminarFoto(foto), Times.Once);
        _archivos.Verify(a => a.EliminarAsync("perfiles/a/b/c.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EliminarFoto_DeOtroPerfil_NoLaEncuentra()
    {
        PerfilExistente();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.EliminarFotoAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ReordenarFotos_AsignaElOrdenPorPosicion()
    {
        var perfil = PerfilExistente();
        var a = new FotoPerfil { Url = "u", Ruta = "a", Orden = 0 };
        var b = new FotoPerfil { Url = "u", Ruta = "b", Orden = 1 };
        perfil.Fotos.Add(a);
        perfil.Fotos.Add(b);

        await _service.ReordenarFotosAsync([b.Id, a.Id]);

        Assert.Equal(0, b.Orden);
        Assert.Equal(1, a.Orden);
    }

    [Fact]
    public async Task ReordenarFotos_ConUnaListaIncompleta_LaRechaza()
    {
        var perfil = PerfilExistente();
        var a = new FotoPerfil { Url = "u", Ruta = "a", Orden = 0 };
        perfil.Fotos.Add(a);
        perfil.Fotos.Add(new FotoPerfil { Url = "u", Ruta = "b", Orden = 1 });

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.ReordenarFotosAsync([a.Id]));
    }

    // ── La trayectoria ──

    [Fact]
    public async Task AgregarHito_AnioAbsurdo_LoRechaza()
    {
        PerfilExistente();
        var dto = new GuardarHitoDto { Anio = 1492, Titulo = "Descubrimiento" };

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.AgregarHitoAsync(dto));
    }

    [Fact]
    public async Task AgregarHito_AnioMuyAdelante_LoRechaza()
    {
        PerfilExistente();
        var dto = new GuardarHitoDto { Anio = DateTime.UtcNow.Year + 2, Titulo = "Todavía no pasó" };

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.AgregarHitoAsync(dto));
    }

    [Fact]
    public async Task AgregarHito_SinTitulo_LoRechaza()
    {
        PerfilExistente();
        var dto = new GuardarHitoDto { Anio = 2015, Titulo = "   " };

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.AgregarHitoAsync(dto));
    }

    [Fact]
    public async Task AgregarHito_PasadoElTope_LoRechaza()
    {
        var perfil = PerfilExistente();
        for (var i = 0; i < 15; i++)
            perfil.Hitos.Add(new HitoTrayectoria { Anio = 2000 + i, Titulo = $"Hito {i}", Orden = i });

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AgregarHitoAsync(new GuardarHitoDto { Anio = 2020, Titulo = "Uno más" }));
    }

    // ── Quién ve el perfil ──

    [Fact]
    public async Task VerPerfil_SinPublicar_NoSeVe()
    {
        var otroTenant = Guid.NewGuid();
        var profe = Guid.NewGuid();
        _repo.Setup(r => r.ObtenerTenantAsync(otroTenant, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Tenant { Id = otroTenant, Subdominio = "x", Nombre = "Club", Estado = EstadoTenant.Activo });
        _repo.Setup(r => r.TrabajaEnElClubAsync(otroTenant, profe, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repo.Setup(r => r.ObtenerDeClubAsync(otroTenant, profe, It.IsAny<CancellationToken>()))
             .ReturnsAsync((new Usuario { Id = profe, Nombre = "Ana", Apellido = "Gómez" },
                            new PerfilProfesor { TenantId = otroTenant, UserId = profe, Publicado = false }));

        Assert.Null(await _service.ObtenerPublicoAsync(otroTenant, profe));
    }

    [Fact]
    public async Task VerPerfil_DeUnProfeQueYaNoTrabajaAhi_NoSeVe()
    {
        var otroTenant = Guid.NewGuid();
        var profe = Guid.NewGuid();
        _repo.Setup(r => r.ObtenerTenantAsync(otroTenant, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Tenant { Id = otroTenant, Subdominio = "x", Nombre = "Club", Estado = EstadoTenant.Activo });
        _repo.Setup(r => r.TrabajaEnElClubAsync(otroTenant, profe, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Assert.Null(await _service.ObtenerPublicoAsync(otroTenant, profe));
    }

    [Fact]
    public async Task VerPerfil_DeUnClubSuspendido_NoSeVe()
    {
        var otroTenant = Guid.NewGuid();
        var profe = Guid.NewGuid();
        _repo.Setup(r => r.ObtenerTenantAsync(otroTenant, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Tenant { Id = otroTenant, Subdominio = "x", Nombre = "Club", Estado = EstadoTenant.Suspendido });

        Assert.Null(await _service.ObtenerPublicoAsync(otroTenant, profe));
    }

    [Fact]
    public async Task VerPerfil_PublicadoYVigente_DevuelveFotosEHitosEnOrden()
    {
        var otroTenant = Guid.NewGuid();
        var profe = Guid.NewGuid();
        var perfil = new PerfilProfesor
        {
            TenantId = otroTenant, UserId = profe, Publicado = true, Titular = "Profesor Nacional",
            Fotos =
            [
                new FotoPerfil { Url = "b.jpg", Ruta = "b", Orden = 1 },
                new FotoPerfil { Url = "a.jpg", Ruta = "a", Orden = 0 },
            ],
            Hitos =
            [
                new HitoTrayectoria { Anio = 2015, Titulo = "Segundo", Orden = 1 },
                new HitoTrayectoria { Anio = 2010, Titulo = "Primero", Orden = 0 },
            ],
        };
        _repo.Setup(r => r.ObtenerTenantAsync(otroTenant, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Tenant { Id = otroTenant, Subdominio = "x", Nombre = "Club Norte", Estado = EstadoTenant.Activo });
        _repo.Setup(r => r.TrabajaEnElClubAsync(otroTenant, profe, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repo.Setup(r => r.ObtenerDeClubAsync(otroTenant, profe, It.IsAny<CancellationToken>()))
             .ReturnsAsync((new Usuario { Id = profe, Nombre = "Ana", Apellido = "Gómez" }, perfil));

        var dto = await _service.ObtenerPublicoAsync(otroTenant, profe);

        Assert.NotNull(dto);
        Assert.Equal("Club Norte", dto!.Club);
        Assert.Equal("Profesor Nacional", dto.Titular);
        Assert.Equal(["a.jpg", "b.jpg"], dto.Fotos.Select(f => f.Url));
        Assert.Equal(["Primero", "Segundo"], dto.Hitos.Select(h => h.Titulo));
    }

    // ── Limpieza al borrar al profe del club ──

    [Fact]
    public async Task EliminarPerfilDeUsuario_SeLlevaTodosSusArchivos()
    {
        var otro = Guid.NewGuid();
        var perfil = new PerfilProfesor
        {
            TenantId = TenantId, UserId = otro,
            AvatarRuta = "perfiles/t/u/avatar.jpg",
            PortadaRuta = "perfiles/t/u/portada.jpg",
            Fotos = [new FotoPerfil { Url = "u", Ruta = "perfiles/t/u/galeria.jpg", Orden = 0 }],
        };
        _repo.Setup(r => r.ObtenerDeUsuarioAsync(otro, It.IsAny<CancellationToken>())).ReturnsAsync(perfil);

        await _service.EliminarPerfilDeUsuarioAsync(otro);

        _repo.Verify(r => r.Eliminar(perfil), Times.Once);
        _archivos.Verify(a => a.EliminarAsync("perfiles/t/u/avatar.jpg", It.IsAny<CancellationToken>()), Times.Once);
        _archivos.Verify(a => a.EliminarAsync("perfiles/t/u/portada.jpg", It.IsAny<CancellationToken>()), Times.Once);
        _archivos.Verify(a => a.EliminarAsync("perfiles/t/u/galeria.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EliminarPerfilDeUsuario_SinPerfil_NoHaceNada()
    {
        var otro = Guid.NewGuid();
        _repo.Setup(r => r.ObtenerDeUsuarioAsync(otro, It.IsAny<CancellationToken>())).ReturnsAsync((PerfilProfesor?)null);

        await _service.EliminarPerfilDeUsuarioAsync(otro);

        _repo.Verify(r => r.Eliminar(It.IsAny<PerfilProfesor>()), Times.Never);
    }

    [Fact]
    public async Task EliminarFoto_SiElStorageFalla_LaFotoSeBorraIgual()
    {
        var perfil = PerfilExistente();
        var foto = new FotoPerfil { Url = "u", Ruta = "perfiles/a/b/c.jpg", Orden = 0 };
        perfil.Fotos.Add(foto);
        _archivos.Setup(a => a.EliminarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new HttpRequestException("se cayó el storage"));

        // Un archivo huérfano es invisible; una foto rota en la pantalla, no
        await _service.EliminarFotoAsync(foto.Id);

        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
