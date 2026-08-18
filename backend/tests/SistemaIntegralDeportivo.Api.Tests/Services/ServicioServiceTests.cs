using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// El catálogo del profe y las FOTOS de cada producto. Lo que se prueba acá es lo que
/// tiene consecuencias: que la imagen se valide por sus bytes (no por lo que declara el
/// cliente), que haya un tope por producto, y que borrar una foto borre también el
/// archivo del storage — si no, se acumulan huérfanos que nadie va a limpiar.
/// </summary>
public class ServicioServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly Mock<IServicioRepository> _servicios = new();
    private readonly Mock<IAlmacenamientoArchivos> _archivos = new();
    private readonly Mock<ITenantActual> _tenantActual = new();
    private readonly ServicioService _service;

    public ServicioServiceTests()
    {
        _service = new ServicioService(
            _servicios.Object, _archivos.Object, _tenantActual.Object,
            NullLogger<ServicioService>.Instance);

        _tenantActual.Setup(t => t.TenantId).Returns(TenantId);
        _archivos.Setup(a => a.SubirAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Stream _, string __, string ruta, CancellationToken ___) => $"https://cdn/{ruta}");
    }

    /// <summary>Bytes con la firma real de un JPEG: el service mira el contenido, no el nombre.</summary>
    private static ImagenSubida Jpeg(int bytes = 100)
    {
        var contenido = new byte[bytes];
        contenido[0] = 0xFF; contenido[1] = 0xD8; contenido[2] = 0xFF;
        return new ImagenSubida(contenido);
    }

    /// <summary>Un producto en el catálogo, con las fotos que se le pasen.</summary>
    private Servicio EnCatalogo(params FotoServicio[] fotos)
    {
        var servicio = new Servicio { TenantId = TenantId, Nombre = "Raqueta Wilson", Precio = 250_000m };
        foreach (var f in fotos) servicio.Fotos.Add(f);
        _servicios.Setup(s => s.ObtenerAsync(servicio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(servicio);
        return servicio;
    }

    private static FotoServicio Foto(int orden, string ruta = "productos/x/vieja.jpg") =>
        new() { Url = $"https://cdn/{ruta}", Ruta = ruta, Orden = orden };

    // ── Subir ──

    [Fact]
    public async Task AgregarFoto_LaSumaAlFinalDeLaGaleria()
    {
        var servicio = EnCatalogo(Foto(0), Foto(1));

        var dto = await _service.AgregarFotoAsync(servicio.Id, Jpeg());

        Assert.Equal(3, servicio.Fotos.Count);
        Assert.Equal(2, dto.Orden); // al final: la primera sigue siendo la del listado
        _servicios.Verify(s => s.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AgregarFoto_LaDaDeAltaEnElRepositorio_NoSoloEnLaColeccion()
    {
        // Esto ya nos rompió en la cara: sumarla SOLO a servicio.Fotos no alcanza. El Id
        // de la foto se asigna en C#, así que EF ve una PK con valor, da por hecho que la
        // fila ya existe y manda un UPDATE de cero filas → DbUpdateConcurrencyException.
        // La misma trampa está documentada en IPerfilProfesorRepository.
        var servicio = EnCatalogo();

        await _service.AgregarFotoAsync(servicio.Id, Jpeg());

        _servicios.Verify(s => s.AgregarFoto(It.IsAny<FotoServicio>()), Times.Once);
    }

    [Fact]
    public async Task AgregarFoto_GuardaLaURL_NoLaImagen()
    {
        // El punto de todo el diseño: en la base va una URL corta, y el archivo al
        // storage. Si esto se rompiera volveríamos al base64 en la fila.
        var servicio = EnCatalogo();

        var dto = await _service.AgregarFotoAsync(servicio.Id, Jpeg());

        var foto = Assert.Single(servicio.Fotos);
        Assert.StartsWith("https://cdn/", dto.Url);
        Assert.StartsWith($"productos/{TenantId}/", foto.Ruta); // la ruta la arma el backend
        _archivos.Verify(a => a.SubirAsync(
            It.IsAny<Stream>(), "image/jpeg", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AgregarFoto_PasadoElTope_Lanza()
    {
        var servicio = EnCatalogo(Foto(0), Foto(1), Foto(2), Foto(3), Foto(4));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AgregarFotoAsync(servicio.Id, Jpeg()));

        _archivos.Verify(a => a.SubirAsync(
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AgregarFoto_QueNoEsImagen_Lanza()
    {
        // Un PDF renombrado a .jpg: se detecta por los bytes, no por lo que dice el cliente.
        var servicio = EnCatalogo();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AgregarFotoAsync(servicio.Id, new ImagenSubida([0x25, 0x50, 0x44, 0x46, 0x2D])));
    }

    [Fact]
    public async Task AgregarFoto_ArchivoVacio_Lanza()
    {
        var servicio = EnCatalogo();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AgregarFotoAsync(servicio.Id, new ImagenSubida([])));
    }

    [Fact]
    public async Task AgregarFoto_ProductoInexistente_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AgregarFotoAsync(Guid.NewGuid(), Jpeg()));
    }

    // ── Borrar ──

    [Fact]
    public async Task BorrarFoto_BorraTambienElArchivoDelStorage()
    {
        var foto = Foto(0, "productos/abc/foto-1.jpg");
        var servicio = EnCatalogo(foto);

        await _service.BorrarFotoAsync(servicio.Id, foto.Id);

        Assert.Empty(servicio.Fotos);
        // La baja va por el repositorio, igual que el alta: sacarla de la colección sola
        // deja que EF decida qué hacer con una hija huérfana.
        _servicios.Verify(s => s.EliminarFoto(foto), Times.Once);
        // Sin esto quedan huérfanos en el storage que nadie va a limpiar nunca.
        _archivos.Verify(a => a.EliminarAsync("productos/abc/foto-1.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BorrarFoto_DeOtroProducto_Lanza()
    {
        var servicio = EnCatalogo(Foto(0));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.BorrarFotoAsync(servicio.Id, Guid.NewGuid()));

        _archivos.Verify(a => a.EliminarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BorrarFoto_SiFallaElStorage_NoRompeLaOperacion()
    {
        // Un huérfano en el storage no lo ve nadie; un error en la cara del profe sí.
        var foto = Foto(0);
        var servicio = EnCatalogo(foto);
        _archivos.Setup(a => a.EliminarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new HttpRequestException("storage caído"));

        await _service.BorrarFotoAsync(servicio.Id, foto.Id); // no lanza

        Assert.Empty(servicio.Fotos);
    }

    // ── Descripción ──

    [Fact]
    public async Task Crear_DescripcionEnBlanco_SeGuardaNull()
    {
        Servicio? creado = null;
        _servicios.Setup(s => s.AgregarAsync(It.IsAny<Servicio>(), It.IsAny<CancellationToken>()))
                  .Callback((Servicio s, CancellationToken _) => creado = s).Returns(Task.CompletedTask);

        await _service.CrearAsync(new GuardarServicioDto { Nombre = "Grip", Descripcion = "   ", Precio = 5_000m });

        Assert.Null(creado!.Descripcion);
    }
}
