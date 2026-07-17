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

/// <summary>
/// A dónde transfiere el alumno: el alias/CBU y el titular. El portal los
/// muestra al informar un pago (null = todavía no cargados).
/// </summary>
public class DatosPagoConfigDto
{
    [StringLength(120)]
    public string? AliasCbu { get; set; }

    [StringLength(120)]
    public string? TitularPago { get; set; }
}

/// <summary>Configuración del tenant: precios del profe y datos de transferencia.</summary>
public interface IConfigService
{
    Task<PreciosDto> ObtenerPreciosAsync(CancellationToken ct = default);
    Task<PreciosDto> ActualizarPreciosAsync(PreciosDto dto, CancellationToken ct = default);
    Task<DatosPagoConfigDto> ObtenerDatosPagoAsync(CancellationToken ct = default);
    Task<DatosPagoConfigDto> ActualizarDatosPagoAsync(DatosPagoConfigDto dto, CancellationToken ct = default);
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

    public async Task<DatosPagoConfigDto> ObtenerDatosPagoAsync(CancellationToken ct = default)
    {
        var tenant = await _tenant.ObtenerActualAsync(ct);
        return new DatosPagoConfigDto
        {
            AliasCbu = tenant.AliasCbu,
            TitularPago = tenant.TitularPago,
        };
    }

    public async Task<DatosPagoConfigDto> ActualizarDatosPagoAsync(
        DatosPagoConfigDto dto, CancellationToken ct = default)
    {
        var tenant = await _tenant.ObtenerActualAsync(ct);
        tenant.AliasCbu = string.IsNullOrWhiteSpace(dto.AliasCbu) ? null : dto.AliasCbu.Trim();
        tenant.TitularPago = string.IsNullOrWhiteSpace(dto.TitularPago) ? null : dto.TitularPago.Trim();
        await _tenant.GuardarCambiosAsync(ct);
        return await ObtenerDatosPagoAsync(ct);
    }
}
