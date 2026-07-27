namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// La costura VOLÁTIL de la facturación (el profe cambia el modelo de cobro seguido):
/// "cómo nacen los cargos del mes". El resto de la app (pagos, morosidad, portal,
/// reportes) lee el ledger de <see cref="Models.Cargo"/> y NO sabe cómo nacieron, así
/// que cambiar el modelo = una implementación nueva + una línea de DI en Program.cs,
/// sin tocar <see cref="CuotaService"/>. Ver docs/adr (política de cuota intercambiable).
/// </summary>
public interface IPoliticaDeCuota
{
    /// <summary>
    /// Asegura (idempotente) los cargos del mes para los alumnos del tenant actual.
    /// La llama <see cref="CuotaService.ObtenerMesAsync"/> antes de liquidar.
    /// </summary>
    Task GenerarCargosDelMesAsync(int anio, int mes, CancellationToken ct = default);
}
