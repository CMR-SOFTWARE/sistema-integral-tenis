using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Noticias del club: las publica el director, las ven todos sus alumnos.</summary>
public interface INoticiaService
{
    /// <summary>soloVigentes=true (portal del alumno): activas y no vencidas. false: todas (profe).</summary>
    Task<IReadOnlyList<NoticiaDto>> ListarAsync(bool soloVigentes, CancellationToken ct = default);
    Task<NoticiaDto> CrearAsync(GuardarNoticiaDto dto, CancellationToken ct = default);
    /// <summary>El que la publicó la corrige (título, mensaje, importancia y vencimiento).</summary>
    Task<NoticiaDto> EditarAsync(Guid id, GuardarNoticiaDto dto, CancellationToken ct = default);
    /// <summary>Baja/reactivación (no se borra; la noticia puede volver a prenderse).</summary>
    Task<NoticiaDto> CambiarActivoAsync(Guid id, bool activo, CancellationToken ct = default);
    Task EliminarAsync(Guid id, CancellationToken ct = default);
}

public class NoticiaService : INoticiaService
{
    private readonly INoticiaRepository _noticias;

    public NoticiaService(INoticiaRepository noticias)
    {
        _noticias = noticias;
    }

    public async Task<IReadOnlyList<NoticiaDto>> ListarAsync(bool soloVigentes, CancellationToken ct = default)
    {
        var noticias = await _noticias.ListarAsync(soloActivas: soloVigentes, ct);
        if (soloVigentes)
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            noticias = noticias.Where(n => n.VenceEl is null || n.VenceEl >= hoy).ToList();
        }
        return noticias.Select(Mapear).ToList();
    }

    public async Task<NoticiaDto> CrearAsync(GuardarNoticiaDto dto, CancellationToken ct = default)
    {
        ValidarVencimiento(dto);

        var noticia = new Noticia
        {
            Titulo = dto.Titulo.Trim(),
            Mensaje = dto.Mensaje.Trim(),
            Importante = dto.Importante,
            VenceEl = dto.VenceEl,
            // TenantId lo asigna el repositorio
        };
        await _noticias.AgregarAsync(noticia, ct);
        await _noticias.GuardarCambiosAsync(ct);
        return Mapear(noticia);
    }

    public async Task<NoticiaDto> EditarAsync(Guid id, GuardarNoticiaDto dto, CancellationToken ct = default)
    {
        var noticia = await _noticias.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("La noticia no existe.");

        ValidarVencimiento(dto);

        noticia.Titulo = dto.Titulo.Trim();
        noticia.Mensaje = dto.Mensaje.Trim();
        noticia.Importante = dto.Importante;
        noticia.VenceEl = dto.VenceEl;
        // Activo no se toca acá: prender y apagar tiene su propio endpoint, así que
        // corregir un título no revive sin querer una noticia que el profe bajó.
        await _noticias.GuardarCambiosAsync(ct);
        return Mapear(noticia);
    }

    public async Task<NoticiaDto> CambiarActivoAsync(Guid id, bool activo, CancellationToken ct = default)
    {
        var noticia = await _noticias.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("La noticia no existe.");

        noticia.Activo = activo;
        await _noticias.GuardarCambiosAsync(ct);
        return Mapear(noticia);
    }

    public async Task EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var noticia = await _noticias.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("La noticia no existe.");

        _noticias.Eliminar(noticia);
        await _noticias.GuardarCambiosAsync(ct);
    }

    /// <summary>Publicar algo que ya nació vencido no lo vería nadie.</summary>
    private static void ValidarVencimiento(GuardarNoticiaDto dto)
    {
        if (dto.VenceEl is { } vence && vence < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ReglaDeNegocioException("La fecha de vencimiento ya pasó.");
    }

    private static NoticiaDto Mapear(Noticia n) => new()
    {
        Id = n.Id,
        Titulo = n.Titulo,
        Mensaje = n.Mensaje,
        Importante = n.Importante,
        VenceEl = n.VenceEl,
        Activo = n.Activo,
        CreadoEl = n.CreadoEl,
    };
}
