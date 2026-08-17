using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// El catálogo del profe: lo que ofrece, con precio, descripción y fotos. Se llama
/// "Servicio" desde M4, cuando eran encordados y tubos de pelotas; en pantalla es
/// **Productos**, porque ahora también se venden raquetas y merch. El nombre en el
/// código no se renombró a propósito: sería un rename de tabla con datos en producción
/// y no aporta lo suficiente.
/// </summary>
public interface IServicioService
{
    Task<IReadOnlyList<ServicioDto>> ListarAsync(bool soloActivos, CancellationToken ct = default);
    Task<ServicioDto> CrearAsync(GuardarServicioDto dto, CancellationToken ct = default);
    Task<ServicioDto> EditarAsync(Guid id, GuardarServicioDto dto, CancellationToken ct = default);
    /// <summary>Baja/reactivación LÓGICA: no se borra (los pedidos históricos lo referencian).</summary>
    Task<ServicioDto> CambiarActivoAsync(Guid id, bool activo, CancellationToken ct = default);

    /// <summary>Suma una foto al producto (la imagen va al storage; en la base queda su URL).</summary>
    Task<FotoServicioDto> AgregarFotoAsync(Guid servicioId, ImagenSubida imagen, CancellationToken ct = default);

    /// <summary>Borra la foto y su archivo del storage.</summary>
    Task BorrarFotoAsync(Guid servicioId, Guid fotoId, CancellationToken ct = default);
}

public class ServicioService : IServicioService
{
    /// <summary>Tope por producto: sin esto alguien sube veinte y el catálogo se arrastra.</summary>
    private const int MaximoFotos = 5;
    private const int TamañoMaximoBytes = 5 * 1024 * 1024;

    private readonly IServicioRepository _servicios;
    private readonly IAlmacenamientoArchivos _archivos;
    private readonly ITenantActual _tenantActual;
    private readonly ILogger<ServicioService> _log;

    public ServicioService(
        IServicioRepository servicios, IAlmacenamientoArchivos archivos,
        ITenantActual tenantActual, ILogger<ServicioService> log)
    {
        _servicios = servicios;
        _archivos = archivos;
        _tenantActual = tenantActual;
        _log = log;
    }

    public async Task<IReadOnlyList<ServicioDto>> ListarAsync(bool soloActivos, CancellationToken ct = default)
    {
        var servicios = await _servicios.ListarAsync(soloActivos, ct);
        return servicios.Select(Mapear).ToList();
    }

    public async Task<ServicioDto> CrearAsync(GuardarServicioDto dto, CancellationToken ct = default)
    {
        var servicio = new Servicio
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = Limpiar(dto.Descripcion),
            Precio = dto.Precio,
            // TenantId lo asigna el repositorio
        };
        await _servicios.AgregarAsync(servicio, ct);
        await _servicios.GuardarCambiosAsync(ct);
        return Mapear(servicio);
    }

    public async Task<ServicioDto> EditarAsync(Guid id, GuardarServicioDto dto, CancellationToken ct = default)
    {
        var servicio = await _servicios.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("El servicio no existe.");

        // Editar el precio NO toca los pedidos ya hechos (guardan su snapshot)
        servicio.Nombre = dto.Nombre.Trim();
        servicio.Descripcion = Limpiar(dto.Descripcion);
        servicio.Precio = dto.Precio;
        await _servicios.GuardarCambiosAsync(ct);
        return Mapear(servicio);
    }

    public async Task<ServicioDto> CambiarActivoAsync(Guid id, bool activo, CancellationToken ct = default)
    {
        var servicio = await _servicios.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("El servicio no existe.");

        servicio.Activo = activo;
        await _servicios.GuardarCambiosAsync(ct);
        return Mapear(servicio);
    }

    // ── Fotos del producto ──

    public async Task<FotoServicioDto> AgregarFotoAsync(
        Guid servicioId, ImagenSubida imagen, CancellationToken ct = default)
    {
        var servicio = await _servicios.ObtenerAsync(servicioId, ct)
            ?? throw new ReglaDeNegocioException("El producto no existe.");

        if (servicio.Fotos.Count >= MaximoFotos)
            throw new ReglaDeNegocioException($"Ya tiene {MaximoFotos} fotos: borrá alguna para subir otra.");

        var (url, ruta) = await GuardarImagenAsync(imagen, ct);
        var foto = new FotoServicio
        {
            ServicioId = servicio.Id,
            Url = url,
            Ruta = ruta,
            Orden = servicio.Fotos.Count == 0 ? 0 : servicio.Fotos.Max(f => f.Orden) + 1,
        };
        // Las dos cosas: el repositorio la da de ALTA (si no, EF ve una PK ya puesta y
        // manda un UPDATE de cero filas) y la colección la deja al día en memoria.
        _servicios.AgregarFoto(foto);
        servicio.Fotos.Add(foto);
        await _servicios.GuardarCambiosAsync(ct);

        return new FotoServicioDto { Id = foto.Id, Url = foto.Url, Orden = foto.Orden };
    }

    public async Task BorrarFotoAsync(Guid servicioId, Guid fotoId, CancellationToken ct = default)
    {
        var servicio = await _servicios.ObtenerAsync(servicioId, ct)
            ?? throw new ReglaDeNegocioException("El producto no existe.");

        var foto = servicio.Fotos.FirstOrDefault(f => f.Id == fotoId)
            ?? throw new ReglaDeNegocioException("Esa foto no es de este producto.");

        var ruta = foto.Ruta;
        _servicios.EliminarFoto(foto);
        servicio.Fotos.Remove(foto);
        await _servicios.GuardarCambiosAsync(ct);
        await BorrarArchivoAsync(ruta, ct);
    }

    /// <summary>
    /// Valida los BYTES (no lo que declara el cliente) y guarda bajo la carpeta del
    /// tenant. La ruta la arma el backend: el cliente no elige ni el nombre ni la carpeta.
    /// Mismo criterio que PerfilProfesorService.
    /// </summary>
    private async Task<(string Url, string Ruta)> GuardarImagenAsync(ImagenSubida imagen, CancellationToken ct)
    {
        if (imagen.Contenido.Length == 0)
            throw new ReglaDeNegocioException("El archivo llegó vacío: probá de nuevo.");
        if (imagen.Contenido.Length > TamañoMaximoBytes)
            throw new ReglaDeNegocioException("La imagen es muy pesada: probá con una más liviana (hasta 5 MB).");

        var contentType = TipoDeImagen.Detectar(imagen.Contenido)
            ?? throw new ReglaDeNegocioException("El archivo tiene que ser una imagen JPG, PNG o WEBP.");

        var ruta = $"productos/{_tenantActual.TenantId}/foto-{Guid.NewGuid():N}.{TipoDeImagen.ExtensionDe(contentType)}";
        using var contenido = new MemoryStream(imagen.Contenido);
        var url = await _archivos.SubirAsync(contenido, contentType, ruta, ct);
        return (url, ruta);
    }

    /// <summary>
    /// El archivo se borra DESPUÉS de la base y sin cortar el request si falla: un
    /// huérfano en el storage no lo ve nadie, una foto rota en la pantalla sí.
    /// </summary>
    private async Task BorrarArchivoAsync(string ruta, CancellationToken ct)
    {
        try
        {
            await _archivos.EliminarAsync(ruta, ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Quedó un archivo huérfano en el storage: {Ruta}", ruta);
        }
    }

    private static string? Limpiar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static ServicioDto Mapear(Servicio s) => new()
    {
        Id = s.Id,
        Nombre = s.Nombre,
        Descripcion = s.Descripcion,
        Precio = s.Precio,
        Activo = s.Activo,
        Fotos = s.Fotos
            .OrderBy(f => f.Orden)
            .Select(f => new FotoServicioDto { Id = f.Id, Url = f.Url, Orden = f.Orden })
            .ToList(),
    };
}
