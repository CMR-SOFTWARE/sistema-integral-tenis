using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Banners de publicidad del tenant (M6): los carga el profe, los ve el alumno.</summary>
public interface IPublicidadService
{
    Task<IReadOnlyList<PublicidadDto>> ListarAsync(bool soloActivas, CancellationToken ct = default);
    Task<PublicidadDto> CrearAsync(GuardarPublicidadDto dto, CancellationToken ct = default);
    /// <summary>Baja/reactivación (no se borra; el banner puede volver a prenderse).</summary>
    Task<PublicidadDto> CambiarActivoAsync(Guid id, bool activo, CancellationToken ct = default);
    Task EliminarAsync(Guid id, CancellationToken ct = default);
}

public class PublicidadService : IPublicidadService
{
    private readonly IPublicidadRepository _publicidad;

    public PublicidadService(IPublicidadRepository publicidad)
    {
        _publicidad = publicidad;
    }

    public async Task<IReadOnlyList<PublicidadDto>> ListarAsync(bool soloActivas, CancellationToken ct = default)
    {
        var banners = await _publicidad.ListarAsync(soloActivas, ct);
        return banners.Select(Mapear).ToList();
    }

    public async Task<PublicidadDto> CrearAsync(GuardarPublicidadDto dto, CancellationToken ct = default)
    {
        // La imagen viene comprimida como data URL (sin storage externo)
        if (!dto.ImagenUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            throw new ReglaDeNegocioException("El banner tiene que ser una imagen.");
        if (dto.ImagenUrl.Length > 700_000) // ~500 KB en base64
            throw new ReglaDeNegocioException("La imagen es muy pesada: probá con una más liviana.");

        var publicidad = new Publicidad
        {
            Nombre = dto.Nombre.Trim(),
            ImagenUrl = dto.ImagenUrl,
            Enlace = string.IsNullOrWhiteSpace(dto.Enlace) ? null : dto.Enlace.Trim(),
            // TenantId lo asigna el repositorio
        };
        await _publicidad.AgregarAsync(publicidad, ct);
        await _publicidad.GuardarCambiosAsync(ct);
        return Mapear(publicidad);
    }

    public async Task<PublicidadDto> CambiarActivoAsync(Guid id, bool activo, CancellationToken ct = default)
    {
        var publicidad = await _publicidad.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("El banner no existe.");

        publicidad.Activo = activo;
        await _publicidad.GuardarCambiosAsync(ct);
        return Mapear(publicidad);
    }

    public async Task EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var publicidad = await _publicidad.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("El banner no existe.");

        _publicidad.Eliminar(publicidad);
        await _publicidad.GuardarCambiosAsync(ct);
    }

    private static PublicidadDto Mapear(Publicidad p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        ImagenUrl = p.ImagenUrl,
        Enlace = p.Enlace,
        Activo = p.Activo,
    };
}
