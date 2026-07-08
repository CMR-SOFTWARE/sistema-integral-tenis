using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class SedeService : ISedeService
{
    private readonly ISedeRepository _sedes;

    public SedeService(ISedeRepository sedes)
    {
        _sedes = sedes;
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
