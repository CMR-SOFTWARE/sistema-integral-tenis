using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Avisos generales del tenant: los carga el profe, los ven todos sus alumnos.</summary>
public interface IAvisoService
{
    /// <summary>soloVigentes=true (portal del alumno): activos y no vencidos. false: todos (profe).</summary>
    Task<IReadOnlyList<AvisoDto>> ListarAsync(bool soloVigentes, CancellationToken ct = default);
    Task<AvisoDto> CrearAsync(GuardarAvisoDto dto, CancellationToken ct = default);
    /// <summary>Baja/reactivación (no se borra; el aviso puede volver a prenderse).</summary>
    Task<AvisoDto> CambiarActivoAsync(Guid id, bool activo, CancellationToken ct = default);
    Task EliminarAsync(Guid id, CancellationToken ct = default);
}

public class AvisoService : IAvisoService
{
    private readonly IAvisoRepository _avisos;

    public AvisoService(IAvisoRepository avisos)
    {
        _avisos = avisos;
    }

    public async Task<IReadOnlyList<AvisoDto>> ListarAsync(bool soloVigentes, CancellationToken ct = default)
    {
        var avisos = await _avisos.ListarAsync(soloActivos: soloVigentes, ct);
        if (soloVigentes)
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            avisos = avisos.Where(a => a.VenceEl is null || a.VenceEl >= hoy).ToList();
        }
        return avisos.Select(Mapear).ToList();
    }

    public async Task<AvisoDto> CrearAsync(GuardarAvisoDto dto, CancellationToken ct = default)
    {
        if (dto.VenceEl is { } vence && vence < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ReglaDeNegocioException("La fecha de vencimiento ya pasó.");

        var aviso = new Aviso
        {
            Titulo = dto.Titulo.Trim(),
            Mensaje = dto.Mensaje.Trim(),
            VenceEl = dto.VenceEl,
            // TenantId lo asigna el repositorio
        };
        await _avisos.AgregarAsync(aviso, ct);
        await _avisos.GuardarCambiosAsync(ct);
        return Mapear(aviso);
    }

    public async Task<AvisoDto> CambiarActivoAsync(Guid id, bool activo, CancellationToken ct = default)
    {
        var aviso = await _avisos.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("El aviso no existe.");

        aviso.Activo = activo;
        await _avisos.GuardarCambiosAsync(ct);
        return Mapear(aviso);
    }

    public async Task EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var aviso = await _avisos.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("El aviso no existe.");

        _avisos.Eliminar(aviso);
        await _avisos.GuardarCambiosAsync(ct);
    }

    private static AvisoDto Mapear(Aviso a) => new()
    {
        Id = a.Id,
        Titulo = a.Titulo,
        Mensaje = a.Mensaje,
        VenceEl = a.VenceEl,
        Activo = a.Activo,
        CreadoEl = a.CreadoEl,
    };
}
