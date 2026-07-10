using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Contrato de datos de la cuenta corriente (cargos).</summary>
public interface ICargoRepository
{
    /// <summary>Cargos del tenant en el mes, con su Alumno (para agrupar la liquidación).</summary>
    Task<IReadOnlyList<Cargo>> ListarDelMesAsync(int anio, int mes, CancellationToken ct = default);

    Task<Cargo?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task AgregarAsync(Cargo cargo, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
