using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>Contrato de datos de la cuenta corriente (cargos).</summary>
public interface ICargoRepository
{
    /// <summary>Cargos del tenant en el mes, con su Alumno (para agrupar la liquidación).</summary>
    Task<IReadOnlyList<Cargo>> ListarDelMesAsync(int anio, int mes, CancellationToken ct = default);

    Task<Cargo?> ObtenerAsync(Guid id, CancellationToken ct = default);

    /// <summary>Cargos generados desde esos turnos (para limpiar al desactivar un horario).</summary>
    Task<IReadOnlyList<Cargo>> ListarPorTurnosAsync(
        IReadOnlyCollection<Guid> turnoIds, CancellationToken ct = default);

    /// <summary>Cargos IMPAGOS de esos alumnos (para la regla de morosidad).</summary>
    Task<IReadOnlyList<Cargo>> ListarImpagosAsync(
        IReadOnlyCollection<Guid> alumnoIds, CancellationToken ct = default);

    Task AgregarAsync(Cargo cargo, CancellationToken ct = default);

    /// <summary>Marca el cargo para borrar (se persiste con GuardarCambiosAsync).</summary>
    void Eliminar(Cargo cargo);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
