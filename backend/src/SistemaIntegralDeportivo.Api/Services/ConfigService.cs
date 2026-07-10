using System.ComponentModel.DataAnnotations;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class PreciosDto
{
    [Range(0, 10_000_000)]
    public decimal? ValorHoraGrupal { get; set; }

    [Range(0, 10_000_000)]
    public decimal? ValorClaseIndividual { get; set; }
}

/// <summary>Configuración del tenant (por ahora: los precios del profe).</summary>
public interface IConfigService
{
    Task<PreciosDto> ObtenerPreciosAsync(CancellationToken ct = default);
    Task<PreciosDto> ActualizarPreciosAsync(PreciosDto dto, CancellationToken ct = default);
}

public class ConfigService : IConfigService
{
    private readonly ITenantRepository _tenant;

    public ConfigService(ITenantRepository tenant)
    {
        _tenant = tenant;
    }

    public async Task<PreciosDto> ObtenerPreciosAsync(CancellationToken ct = default)
    {
        var tenant = await _tenant.ObtenerActualAsync(ct);
        return new PreciosDto
        {
            ValorHoraGrupal = tenant.ValorHoraGrupal,
            ValorClaseIndividual = tenant.ValorClaseIndividual,
        };
    }

    public async Task<PreciosDto> ActualizarPreciosAsync(PreciosDto dto, CancellationToken ct = default)
    {
        var tenant = await _tenant.ObtenerActualAsync(ct);
        // Solo cambia los precios FUTUROS: los cargos ya generados son snapshot
        tenant.ValorHoraGrupal = dto.ValorHoraGrupal;
        tenant.ValorClaseIndividual = dto.ValorClaseIndividual;
        await _tenant.GuardarCambiosAsync(ct);
        return dto;
    }
}
