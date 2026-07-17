using SistemaIntegralDeportivo.Api.Dtos;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// El portal del ALUMNO: todo se resuelve desde la ficha vinculada al
/// usuario del token (Alumno.UserId) — nunca por parámetros del cliente.
/// </summary>
public interface IPortalService
{
    /// <summary>
    /// Mis clases: próximas (hoy → fin del mes que viene, materializando lo
    /// que falte) e historial (mes pasado → ayer, lo más reciente primero).
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si el usuario no tiene ficha vinculada.</exception>
    Task<MisTurnosDto> MisTurnosAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Mi liquidación del mes (null = sin movimientos).</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si el usuario no tiene ficha vinculada.</exception>
    Task<AlumnoLiquidacionDto?> MiCuotaAsync(Guid userId, int anio, int mes, CancellationToken ct = default);

    /// <summary>Aviso que transferí el mes completo (queda pendiente de que el profe confirme).</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Sin ficha, o nada por informar.</exception>
    Task InformarPagoMesAsync(Guid userId, int anio, int mes, CancellationToken ct = default);

    /// <summary>Aviso que transferí UN cargo puntual (ej: un encordado ya cargado).</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Sin ficha, cargo ajeno/pagado/ya informado.</exception>
    Task InformarPagoCargoAsync(Guid userId, Guid cargoId, CancellationToken ct = default);

    /// <summary>Los datos de transferencia del club (alias/CBU + titular) para el modal de pago.</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si el usuario no tiene ficha vinculada.</exception>
    Task<DatosPagoDto> DatosPagoAsync(Guid userId, CancellationToken ct = default);

    /// <summary>El catálogo de servicios activos del club (lo que puedo pedir).</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si el usuario no tiene ficha vinculada.</exception>
    Task<IReadOnlyList<ServicioDto>> ServiciosAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Pido un servicio del catálogo (queda Pendiente hasta que el profe lo acepte).</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Sin ficha, o servicio inexistente/inactivo.</exception>
    Task<PedidoDto> PedirServicioAsync(Guid userId, Guid servicioId, CancellationToken ct = default);

    /// <summary>Mis pedidos con su estado (Pendiente/Aceptado/Rechazado).</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si el usuario no tiene ficha vinculada.</exception>
    Task<IReadOnlyList<PedidoDto>> MisPedidosAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Mi ficha, como me ve el club.</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Si el usuario no tiene ficha vinculada.</exception>
    Task<MiPerfilDto> MiPerfilAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Edita MIS datos de contacto (teléfono/email); el resto es del profe.</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Sin ficha vinculada o teléfono vacío.</exception>
    Task<MiPerfilDto> ActualizarMiPerfilAsync(
        Guid userId, ActualizarMiPerfilDto dto, CancellationToken ct = default);

    /// <summary>
    /// Aviso de cancelación de MI turno (hasta la hora de inicio): el turno
    /// sigue para el resto y mi cargo queda (falta con aviso; la recuperación
    /// la decide el profe). No mueve plata.
    /// </summary>
    /// <exception cref="Common.ReglaDeNegocioException">
    /// Sin ficha, turno inexistente/ajeno/pasado/cancelado, ya avisado o sin motivo.
    /// </exception>
    Task CancelarMiTurnoAsync(Guid userId, Guid turnoId, string motivo, CancellationToken ct = default);
}
