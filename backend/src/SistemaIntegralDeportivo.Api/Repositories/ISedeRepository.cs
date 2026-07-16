using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Contrato de datos de Sedes y sus canchas.</summary>
public interface ISedeRepository
{
    Task<IReadOnlyList<Sede>> ListarAsync(CancellationToken ct = default);
    Task<Sede?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<Sede> AgregarAsync(Sede sede, CancellationToken ct = default);
    Task<Cancha> AgregarCanchaAsync(Cancha cancha, CancellationToken ct = default);

    /// <summary>Confirma cambios sobre entidades trackeadas (baja/alta lógica).</summary>
    Task GuardarCambiosAsync(CancellationToken ct = default);
}
