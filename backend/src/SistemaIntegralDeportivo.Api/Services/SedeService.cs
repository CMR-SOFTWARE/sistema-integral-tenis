using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class SedeService : ISedeService
{
    private readonly ISedeRepository _sedes;
    private readonly IHorarioRepository _horarios;

    public SedeService(ISedeRepository sedes, IHorarioRepository horarios)
    {
        _sedes = sedes;
        _horarios = horarios;
    }

    public async Task<IReadOnlyList<SedeResponseDto>> ListarAsync(CancellationToken ct = default)
    {
        var sedes = await _sedes.ListarAsync(ct);
        return sedes.Select(Mapear).ToList();
    }

    public async Task<SedeResponseDto> CrearAsync(CreateSedeDto dto, CancellationToken ct = default)
    {
        var sede = await _sedes.AgregarAsync(new Sede { Nombre = dto.Nombre }, ct);
        return Mapear(sede);
    }

    public async Task<CanchaResponseDto> AgregarCanchaAsync(
        Guid sedeId, CreateCanchaDto dto, CancellationToken ct = default)
    {
        var sede = await _sedes.ObtenerAsync(sedeId, ct)
            ?? throw new ReglaDeNegocioException("La sede no existe.");

        var cancha = await _sedes.AgregarCanchaAsync(
            new Cancha { SedeId = sede.Id, Nombre = dto.Nombre }, ct);

        return new CanchaResponseDto { Id = cancha.Id, Nombre = cancha.Nombre, Activo = cancha.Activo };
    }

    public async Task DesactivarAsync(Guid id, CancellationToken ct = default)
    {
        var sede = await _sedes.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("La sede no existe.");

        // Una sede en uso no se da de baja "por sorpresa": el profe primero
        // decide qué hacer con esos horarios (desactivarlos limpia sus turnos)
        var canchas = sede.Canchas.Select(c => c.Id).ToHashSet();
        var activos = await _horarios.ListarActivosAsync(ct);
        if (activos.Any(h => canchas.Contains(h.CanchaId)))
            throw new ReglaDeNegocioException(
                "La sede tiene horarios activos: desactivalos primero (Horarios) y volvé a intentar.");

        // Baja LÓGICA: nunca se borra (la historia de turnos la referencia)
        sede.Activo = false;
        await _sedes.GuardarCambiosAsync(ct);
    }

    public async Task ReactivarAsync(Guid id, CancellationToken ct = default)
    {
        var sede = await _sedes.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("La sede no existe.");

        sede.Activo = true;
        await _sedes.GuardarCambiosAsync(ct);
    }

    private static SedeResponseDto Mapear(Sede s) => new()
    {
        Id = s.Id,
        Nombre = s.Nombre,
        Activo = s.Activo,
        Canchas = s.Canchas
            .OrderBy(c => c.Nombre)
            .Select(c => new CanchaResponseDto { Id = c.Id, Nombre = c.Nombre, Activo = c.Activo })
            .ToList(),
    };
}
