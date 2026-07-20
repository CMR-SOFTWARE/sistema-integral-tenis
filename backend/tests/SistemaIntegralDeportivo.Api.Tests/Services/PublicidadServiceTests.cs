using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Publicidad (M6, TDD): la regla es la validación de la imagen (data URL) al
/// cargar el banner; el resto es CRUD + baja/reactivación.
/// </summary>
public class PublicidadServiceTests
{
    private readonly Mock<IPublicidadRepository> _repo;
    private readonly PublicidadService _service;

    public PublicidadServiceTests()
    {
        _repo = new Mock<IPublicidadRepository>();
        _service = new PublicidadService(_repo.Object);
    }

    private static GuardarPublicidadDto Dto(string imagen) => new()
    {
        Nombre = "Deportes García",
        ImagenUrl = imagen,
        Enlace = "https://deportesgarcia.com",
    };

    [Fact]
    public async Task Crear_ImagenValida_CreaElBannerActivo()
    {
        Publicidad? creado = null;
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Publicidad>(), It.IsAny<CancellationToken>()))
             .Callback((Publicidad p, CancellationToken _) => creado = p).Returns(Task.CompletedTask);

        var res = await _service.CrearAsync(Dto("data:image/jpeg;base64,/9j/abc123"));

        Assert.NotNull(creado);
        Assert.True(creado!.Activo);
        Assert.Equal("Deportes García", creado.Nombre);
        Assert.Equal("https://deportesgarcia.com", creado.Enlace);
        Assert.True(res.Activo);
    }

    [Fact]
    public async Task Crear_NoEsImagen_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(Dto("data:text/html;base64,PHNjcmlwdD4=")));

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Publicidad>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Crear_MuyPesada_Lanza()
    {
        var enorme = "data:image/jpeg;base64," + new string('A', 700_001);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(Dto(enorme)));
    }

    [Fact]
    public async Task CambiarActivo_ApagaElBanner()
    {
        var banner = new Publicidad { Nombre = "x", ImagenUrl = "data:image/png;base64,a", Activo = true };
        _repo.Setup(r => r.ObtenerAsync(banner.Id, It.IsAny<CancellationToken>())).ReturnsAsync(banner);

        await _service.CambiarActivoAsync(banner.Id, false);

        Assert.False(banner.Activo);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Eliminar_BorraElBanner()
    {
        var banner = new Publicidad { Nombre = "x", ImagenUrl = "data:image/png;base64,a" };
        _repo.Setup(r => r.ObtenerAsync(banner.Id, It.IsAny<CancellationToken>())).ReturnsAsync(banner);

        await _service.EliminarAsync(banner.Id);

        _repo.Verify(r => r.Eliminar(banner), Times.Once);
    }

    [Fact]
    public async Task Eliminar_Inexistente_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.EliminarAsync(Guid.NewGuid()));
    }
}
