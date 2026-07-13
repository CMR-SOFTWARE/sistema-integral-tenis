using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Contrato de datos de bloqueos de agenda.</summary>
public interface IBloqueoRepository
{
    /// <summary>Todos los bloqueos del tenant, con su Cancha.</summary>
    Task<IReadOnlyList<Bloqueo>> ListarAsync(CancellationToken ct = default);

    Task<Bloqueo?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task AgregarAsync(Bloqueo bloqueo, CancellationToken ct = default);

    /// <summary>Marca el bloqueo para borrar (se persiste con GuardarCambiosAsync).</summary>
    void Eliminar(Bloqueo bloqueo);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
