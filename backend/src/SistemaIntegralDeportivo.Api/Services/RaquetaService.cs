using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Las raquetas del alumno (M3): las administra él mismo desde su perfil.</summary>
public interface IRaquetaService
{
    Task<IReadOnlyList<RaquetaDto>> MisAsync(Guid alumnoId, CancellationToken ct = default);
    Task<RaquetaDto> AgregarAsync(Guid alumnoId, GuardarRaquetaDto dto, CancellationToken ct = default);
    Task<RaquetaDto> EditarAsync(Guid alumnoId, Guid raquetaId, GuardarRaquetaDto dto, CancellationToken ct = default);
    Task BorrarAsync(Guid alumnoId, Guid raquetaId, CancellationToken ct = default);
}

public class RaquetaService : IRaquetaService
{
    private readonly IRaquetaRepository _raquetas;

    public RaquetaService(IRaquetaRepository raquetas)
    {
        _raquetas = raquetas;
    }

    public async Task<IReadOnlyList<RaquetaDto>> MisAsync(Guid alumnoId, CancellationToken ct = default)
    {
        var raquetas = await _raquetas.ListarPorAlumnoAsync(alumnoId, ct);
        return raquetas.Select(Mapear).ToList();
    }

    public async Task<RaquetaDto> AgregarAsync(
        Guid alumnoId, GuardarRaquetaDto dto, CancellationToken ct = default)
    {
        var raqueta = new Raqueta
        {
            AlumnoId = alumnoId,
            Marca = dto.Marca.Trim(),
            Tension = Limpiar(dto.Tension),
            MarcaEncordado = Limpiar(dto.MarcaEncordado),
            // TenantId lo asigna el repositorio
        };
        await _raquetas.AgregarAsync(raqueta, ct);
        await _raquetas.GuardarCambiosAsync(ct);
        return Mapear(raqueta);
    }

    public async Task<RaquetaDto> EditarAsync(
        Guid alumnoId, Guid raquetaId, GuardarRaquetaDto dto, CancellationToken ct = default)
    {
        var raqueta = await MiaAsync(alumnoId, raquetaId, ct);
        raqueta.Marca = dto.Marca.Trim();
        raqueta.Tension = Limpiar(dto.Tension);
        raqueta.MarcaEncordado = Limpiar(dto.MarcaEncordado);
        await _raquetas.GuardarCambiosAsync(ct);
        return Mapear(raqueta);
    }

    public async Task BorrarAsync(Guid alumnoId, Guid raquetaId, CancellationToken ct = default)
    {
        var raqueta = await MiaAsync(alumnoId, raquetaId, ct);
        _raquetas.Eliminar(raqueta);
        await _raquetas.GuardarCambiosAsync(ct);
    }

    /// <summary>La raqueta tiene que existir y ser DEL alumno (no de otro).</summary>
    private async Task<Raqueta> MiaAsync(Guid alumnoId, Guid raquetaId, CancellationToken ct)
    {
        var raqueta = await _raquetas.ObtenerAsync(raquetaId, ct)
            ?? throw new ReglaDeNegocioException("La raqueta no existe.");
        if (raqueta.AlumnoId != alumnoId)
            throw new ReglaDeNegocioException("Esa raqueta no es tuya.");
        return raqueta;
    }

    private static string? Limpiar(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static RaquetaDto Mapear(Raqueta r) => new()
    {
        Id = r.Id,
        Marca = r.Marca,
        Tension = r.Tension,
        MarcaEncordado = r.MarcaEncordado,
    };
}
