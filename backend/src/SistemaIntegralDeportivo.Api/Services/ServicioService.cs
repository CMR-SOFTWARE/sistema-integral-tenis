using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>El catálogo de servicios del profe (M4): lo que ofrece, con precio.</summary>
public interface IServicioService
{
    Task<IReadOnlyList<ServicioDto>> ListarAsync(bool soloActivos, CancellationToken ct = default);
    Task<ServicioDto> CrearAsync(GuardarServicioDto dto, CancellationToken ct = default);
    Task<ServicioDto> EditarAsync(Guid id, GuardarServicioDto dto, CancellationToken ct = default);
    /// <summary>Baja/reactivación LÓGICA: no se borra (los pedidos históricos lo referencian).</summary>
    Task<ServicioDto> CambiarActivoAsync(Guid id, bool activo, CancellationToken ct = default);
}

public class ServicioService : IServicioService
{
    private readonly IServicioRepository _servicios;

    public ServicioService(IServicioRepository servicios)
    {
        _servicios = servicios;
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

    private static ServicioDto Mapear(Servicio s) => new()
    {
        Id = s.Id,
        Nombre = s.Nombre,
        Precio = s.Precio,
        Activo = s.Activo,
    };
}
