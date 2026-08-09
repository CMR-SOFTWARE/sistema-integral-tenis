using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Las raquetas del alumno y su historial de encordado (M3). Todo recibe el
/// <c>alumnoId</c> y valida que la raqueta sea SUYA: por eso el mismo service sirve
/// a los dos lados —el alumno desde el portal (el id sale de su token) y el profe
/// desde la ficha (el id va por ruta)—, que es como lo pidió el profe: él encorda.
/// </summary>
public interface IRaquetaService
{
    Task<IReadOnlyList<RaquetaDto>> MisAsync(Guid alumnoId, CancellationToken ct = default);
    Task<RaquetaDto> AgregarAsync(Guid alumnoId, GuardarRaquetaDto dto, CancellationToken ct = default);
    Task<RaquetaDto> EditarAsync(Guid alumnoId, Guid raquetaId, GuardarRaquetaDto dto, CancellationToken ct = default);
    Task BorrarAsync(Guid alumnoId, Guid raquetaId, CancellationToken ct = default);

    Task<RaquetaDto> AgregarEncordadoAsync(
        Guid alumnoId, Guid raquetaId, GuardarEncordadoDto dto, CancellationToken ct = default);
    Task<RaquetaDto> EditarEncordadoAsync(
        Guid alumnoId, Guid encordadoId, GuardarEncordadoDto dto, CancellationToken ct = default);
    Task BorrarEncordadoAsync(Guid alumnoId, Guid encordadoId, CancellationToken ct = default);
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
            Modelo = Limpiar(dto.Modelo),
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
        raqueta.Modelo = Limpiar(dto.Modelo);
        await _raquetas.GuardarCambiosAsync(ct);
        return Mapear(raqueta);
    }

    public async Task BorrarAsync(Guid alumnoId, Guid raquetaId, CancellationToken ct = default)
    {
        var raqueta = await MiaAsync(alumnoId, raquetaId, ct);
        _raquetas.Eliminar(raqueta); // los encordados se van en cascada
        await _raquetas.GuardarCambiosAsync(ct);
    }

    // ── El historial de encordados ──

    public async Task<RaquetaDto> AgregarEncordadoAsync(
        Guid alumnoId, Guid raquetaId, GuardarEncordadoDto dto, CancellationToken ct = default)
    {
        var raqueta = await MiaAsync(alumnoId, raquetaId, ct);

        var encordado = Construir(dto);
        encordado.RaquetaId = raqueta.Id;
        await _raquetas.AgregarEncordadoAsync(encordado, ct);
        raqueta.Encordados.Add(encordado); // para devolver la raqueta ya actualizada
        await _raquetas.GuardarCambiosAsync(ct);

        return Mapear(raqueta);
    }

    public async Task<RaquetaDto> EditarEncordadoAsync(
        Guid alumnoId, Guid encordadoId, GuardarEncordadoDto dto, CancellationToken ct = default)
    {
        var encordado = await _raquetas.ObtenerEncordadoAsync(encordadoId, ct)
            ?? throw new ReglaDeNegocioException("Ese encordado no existe.");
        // La pertenencia se valida por la RAQUETA: es la dueña del historial.
        var raqueta = await MiaAsync(alumnoId, encordado.RaquetaId, ct);

        encordado.CuerdaVertical = dto.CuerdaVertical.Trim();
        encordado.TensionVertical = Limpiar(dto.TensionVertical);
        encordado.CuerdaHorizontal = Limpiar(dto.CuerdaHorizontal);
        encordado.TensionHorizontal = Limpiar(dto.TensionHorizontal);
        encordado.Fecha = dto.Fecha;
        await _raquetas.GuardarCambiosAsync(ct);

        // Se relee: la instancia editada y la de la colección pueden ser distintas.
        return Mapear(await MiaAsync(alumnoId, raqueta.Id, ct));
    }

    public async Task BorrarEncordadoAsync(
        Guid alumnoId, Guid encordadoId, CancellationToken ct = default)
    {
        var encordado = await _raquetas.ObtenerEncordadoAsync(encordadoId, ct)
            ?? throw new ReglaDeNegocioException("Ese encordado no existe.");
        await MiaAsync(alumnoId, encordado.RaquetaId, ct);

        _raquetas.EliminarEncordado(encordado);
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

    private static Encordado Construir(GuardarEncordadoDto dto) => new()
    {
        CuerdaVertical = dto.CuerdaVertical.Trim(),
        TensionVertical = Limpiar(dto.TensionVertical),
        CuerdaHorizontal = Limpiar(dto.CuerdaHorizontal),
        TensionHorizontal = Limpiar(dto.TensionHorizontal),
        Fecha = dto.Fecha,
    };

    private static string? Limpiar(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static RaquetaDto Mapear(Raqueta r)
    {
        // Del más nuevo al más viejo. Desempata CreadoEl: dos encordados pueden
        // llevar la misma fecha (se cargó uno mal y se corrigió cargando otro).
        var historial = r.Encordados
            .OrderByDescending(e => e.Fecha).ThenByDescending(e => e.CreadoEl)
            .Select(MapearEncordado)
            .ToList();

        return new RaquetaDto
        {
            Id = r.Id,
            Marca = r.Marca,
            Modelo = r.Modelo,
            Encordados = historial,
            UltimoEncordado = historial.FirstOrDefault(),
        };
    }

    private static EncordadoDto MapearEncordado(Encordado e) => new()
    {
        Id = e.Id,
        CuerdaVertical = e.CuerdaVertical,
        TensionVertical = e.TensionVertical,
        CuerdaHorizontal = e.CuerdaHorizontal,
        TensionHorizontal = e.TensionHorizontal,
        Fecha = e.Fecha,
        EsHibrido = e.EsHibrido,
    };
}
