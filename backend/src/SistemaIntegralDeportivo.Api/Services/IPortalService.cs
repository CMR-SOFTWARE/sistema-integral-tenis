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

    /// <summary>Cuota CONSOLIDADA de la familia (Capa 2b): liquidación de cada miembro + total.</summary>
    Task<CuotaFamiliaDto> MiCuotaFamiliaAsync(Guid userId, int anio, int mes, CancellationToken ct = default);

    /// <summary>Informar el pago del mes de TODA la familia (los miembros que deban).</summary>
    Task InformarPagoFamiliaAsync(Guid userId, int anio, int mes, CancellationToken ct = default);

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

    /// <summary>Edita MIS datos: contacto (teléfono/email) + categoría; el resto es del profe.</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Sin ficha vinculada o teléfono vacío.</exception>
    Task<MiPerfilDto> ActualizarMiPerfilAsync(
        Guid userId, ActualizarMiPerfilDto dto, CancellationToken ct = default);

    /// <summary>Cambia mi foto de perfil (data URL base64) o la quita (null/vacío).</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Sin ficha, no es imagen o muy pesada.</exception>
    Task<MiPerfilDto> ActualizarFotoAsync(Guid userId, string? fotoUrl, CancellationToken ct = default);

    /// <summary>Grupos a los que me podría sumar (cupo + mi categoría), con precio estimado.</summary>
    Task<IReadOnlyList<GrupoDisponibleDto>> GruposDisponiblesAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Pido sumarme a un grupo (queda pendiente de que el profe lo apruebe).</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Sin ficha, grupo lleno/de otra categoría, ya miembro o ya solicitado.</exception>
    Task<SolicitudGrupoDto> SolicitarGrupoAsync(Guid userId, Guid grupoId, CancellationToken ct = default);

    /// <summary>Mis solicitudes de grupo con su estado.</summary>
    Task<IReadOnlyList<SolicitudGrupoDto>> MisSolicitudesGrupoAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Los banners de publicidad activos del club (para el Inicio del portal).</summary>
    Task<IReadOnlyList<PublicidadDto>> PublicidadAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Los avisos generales vigentes del club (para el Inicio del portal).</summary>
    Task<IReadOnlyList<AvisoDto>> AvisosAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Las notas que el profe me compartió (para el Inicio del portal).</summary>
    Task<IReadOnlyList<NotaAlumnoDto>> NotasAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Las sedes del club (para elegir dónde quiero la clase individual).</summary>
    Task<IReadOnlyList<SedeReservaDto>> SedesAsync(Guid userId, CancellationToken ct = default);

    // ── Clase suelta (M5c) ──

    /// <summary>Reservo una clase suelta (individual, una fecha puntual); nace su cargo a pagar.</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Sin ficha, no activo, deuda, fecha pasada, sin cancha o sin precio.</exception>
    Task<ClaseSueltaDto> SolicitarClaseSueltaAsync(
        Guid userId, Guid sedeId, DateOnly fecha, TimeOnly hora, int duracionMinutos, CancellationToken ct = default);

    /// <summary>Mis clases sueltas con su estado y pago.</summary>
    Task<IReadOnlyList<ClaseSueltaDto>> MisClasesSueltasAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Aviso que pagué mi clase suelta (informa el pago del cargo).</summary>
    Task InformarPagoClaseSueltaAsync(Guid userId, Guid claseId, CancellationToken ct = default);

    /// <summary>¿Hay cancha libre en esa sede para una clase suelta ESA fecha/hora?</summary>
    Task<DisponibilidadDto> DisponibilidadClaseSueltaAsync(
        Guid userId, Guid sedeId, DateOnly fecha, TimeOnly hora, int duracionMinutos, CancellationToken ct = default);

    /// <summary>Propongo una clase individual fija (sede + día/hora/duración); el profe elige la cancha.</summary>
    /// <exception cref="Common.ReglaDeNegocioException">Sin ficha, no activo, deuda vencida, sin canchas libres o ya pedido.</exception>
    Task<SolicitudHorarioDto> SolicitarHorarioAsync(
        Guid userId, Guid sedeId, DayOfWeek dia, TimeOnly hora, int duracionMinutos, CancellationToken ct = default);

    /// <summary>Mis solicitudes de clase individual con su estado.</summary>
    Task<IReadOnlyList<SolicitudHorarioDto>> MisSolicitudesHorarioAsync(Guid userId, CancellationToken ct = default);

    /// <summary>¿Hay cancha libre en esa SEDE para una clase individual a ese día/hora?</summary>
    Task<DisponibilidadDto> DisponibilidadHorarioAsync(
        Guid userId, Guid sedeId, DayOfWeek dia, TimeOnly hora, int duracionMinutos, CancellationToken ct = default);

    /// <summary>Mis raquetas.</summary>
    Task<IReadOnlyList<RaquetaDto>> MisRaquetasAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Agrego una raqueta a mi perfil.</summary>
    Task<RaquetaDto> AgregarRaquetaAsync(Guid userId, GuardarRaquetaDto dto, CancellationToken ct = default);

    /// <summary>Edito una raqueta mía.</summary>
    /// <exception cref="Common.ReglaDeNegocioException">La raqueta no existe o no es mía.</exception>
    Task<RaquetaDto> EditarRaquetaAsync(Guid userId, Guid raquetaId, GuardarRaquetaDto dto, CancellationToken ct = default);

    /// <summary>Borro una raqueta mía.</summary>
    /// <exception cref="Common.ReglaDeNegocioException">La raqueta no existe o no es mía.</exception>
    Task BorrarRaquetaAsync(Guid userId, Guid raquetaId, CancellationToken ct = default);

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
