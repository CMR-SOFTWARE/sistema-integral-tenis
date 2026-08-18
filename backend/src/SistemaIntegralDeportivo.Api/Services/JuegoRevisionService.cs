using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Pedido de revisión sobre un partido finalizado. Es un TICKET, no un
/// mecanismo de corrección: ResolverAsync solo marca Resuelta + guarda la
/// respuesta — nunca toca PuntosMovimiento ni GanadorId (por diseño: este
/// servicio ni siquiera tiene forma de escribir esos repos).
/// </summary>
public interface IJuegoRevisionService
{
    Task<JuegoRevisionDto> CrearAsync(
        Guid usuarioId, Guid? juegoPendienteId, Guid? juegoDoblesPendienteId, string comentario,
        CancellationToken ct = default);
    Task ResolverAsync(Guid adminUserId, Guid revisionId, string respuesta, CancellationToken ct = default);

    /// <summary>Todas las Pendientes — para el panel de moderación de Plataforma.</summary>
    Task<IReadOnlyList<RevisionPendienteDto>> ListarPendientesAsync(CancellationToken ct = default);
}

public class JuegoRevisionService : IJuegoRevisionService
{
    private readonly IJuegoRevisionRepository _revisiones;
    private readonly IJuegoPendienteRepository _singlesJuegos;
    private readonly IJuegoDoblesPendienteRepository _doblesJuegos;
    private readonly IJugadorRankingRepository _jugadores;
    private readonly INotificacionService _notificaciones;
    private readonly IAdminRepository _admin;

    public JuegoRevisionService(
        IJuegoRevisionRepository revisiones, IJuegoPendienteRepository singlesJuegos,
        IJuegoDoblesPendienteRepository doblesJuegos, IJugadorRankingRepository jugadores,
        INotificacionService notificaciones, IAdminRepository admin)
    {
        _revisiones = revisiones;
        _singlesJuegos = singlesJuegos;
        _doblesJuegos = doblesJuegos;
        _jugadores = jugadores;
        _notificaciones = notificaciones;
        _admin = admin;
    }

    public async Task<JuegoRevisionDto> CrearAsync(
        Guid usuarioId, Guid? juegoPendienteId, Guid? juegoDoblesPendienteId, string comentario,
        CancellationToken ct = default)
    {
        if ((juegoPendienteId is null) == (juegoDoblesPendienteId is null))
            throw new ReglaDeNegocioException("Tenés que indicar exactamente un partido (singles o dobles).");
        if (comentario.Trim().Length < 10)
            throw new ReglaDeNegocioException("El comentario tiene que tener al menos 10 caracteres.");

        var jugador = await _jugadores.ObtenerPorUsuarioAsync(usuarioId, ct)
            ?? throw new ReglaDeNegocioException("Tenés que estar inscripto en el ranking.");

        var esParticipante = juegoPendienteId is { } singlesId
            ? await EsParticipanteDeSinglesAsync(singlesId, jugador.Id, ct)
            : await EsParticipanteDeDoblesAsync(juegoDoblesPendienteId!.Value, jugador.Id, ct);
        if (!esParticipante)
            throw new ReglaDeNegocioException("No participaste de ese partido.");

        if (await _revisiones.ExistePendienteAsync(juegoPendienteId, juegoDoblesPendienteId, ct))
            throw new ReglaDeNegocioException("Ya hay una revisión pendiente para ese partido.");

        var revision = new JuegoRevision
        {
            JuegoPendienteId = juegoPendienteId,
            JuegoDoblesPendienteId = juegoDoblesPendienteId,
            CreadoPorUserId = usuarioId,
            Comentario = comentario.Trim(),
        };
        await _revisiones.AgregarAsync(revision, ct);
        await _revisiones.GuardarCambiosAsync(ct);

        var admins = await _admin.ListarUsuarioIdsAdminsAsync(ct);
        foreach (var adminUserId in admins)
            await _notificaciones.NotificarAsync(
                adminUserId, "RevisionPedida", "Pidieron revisar el resultado de un partido de ranking.", revision.Id, ct);

        return Mapear(revision);
    }

    public async Task ResolverAsync(Guid adminUserId, Guid revisionId, string respuesta, CancellationToken ct = default)
    {
        var revision = await _revisiones.ObtenerAsync(revisionId, ct)
            ?? throw new ReglaDeNegocioException("La revisión no existe.");
        if (revision.Estado != EstadoJuegoRevision.Pendiente)
            throw new ReglaDeNegocioException("Esa revisión ya se resolvió.");

        revision.Estado = EstadoJuegoRevision.Resuelta;
        revision.RespuestaAdmin = respuesta.Trim();
        revision.ResueltoPorUserId = adminUserId;
        revision.ResueltoEl = DateTime.UtcNow;
        await _revisiones.GuardarCambiosAsync(ct);

        await _notificaciones.NotificarAsync(
            revision.CreadoPorUserId, "RevisionResuelta", "Tu pedido de revisión fue respondido.", revision.Id, ct);
    }

    public async Task<IReadOnlyList<RevisionPendienteDto>> ListarPendientesAsync(CancellationToken ct = default)
    {
        var pendientes = await _revisiones.ListarPendientesAsync(ct);
        var dtos = new List<RevisionPendienteDto>();
        foreach (var r in pendientes)
        {
            var creador = await _jugadores.ObtenerPorUsuarioAsync(r.CreadoPorUserId, ct);
            var fila = creador is null ? null : await _jugadores.ObtenerConNombreAsync(creador.Id, ct);
            dtos.Add(new RevisionPendienteDto
            {
                Id = r.Id,
                JuegoPendienteId = r.JuegoPendienteId,
                JuegoDoblesPendienteId = r.JuegoDoblesPendienteId,
                CreadoPorNombre = fila is null ? "—" : $"{fila.Nombre} {fila.Apellido}",
                Comentario = r.Comentario,
                CreadoEl = r.CreadoEl,
            });
        }
        return dtos;
    }

    private async Task<bool> EsParticipanteDeSinglesAsync(Guid juegoId, Guid jugadorId, CancellationToken ct)
    {
        var juego = await _singlesJuegos.ObtenerAsync(juegoId, ct)
            ?? throw new ReglaDeNegocioException("El partido no existe.");
        if (juego.Estado != EstadoJuegoPendiente.Finalizado)
            throw new ReglaDeNegocioException("Solo se puede pedir revisión de un partido ya finalizado.");
        return juego.Jugador1Id == jugadorId || juego.Jugador2Id == jugadorId;
    }

    private async Task<bool> EsParticipanteDeDoblesAsync(Guid juegoId, Guid jugadorId, CancellationToken ct)
    {
        var juego = await _doblesJuegos.ObtenerAsync(juegoId, ct)
            ?? throw new ReglaDeNegocioException("El partido no existe.");
        if (juego.Estado != EstadoJuegoPendiente.Finalizado)
            throw new ReglaDeNegocioException("Solo se puede pedir revisión de un partido ya finalizado.");
        return juego.Jugador1Id == jugadorId || juego.Jugador2Id == jugadorId
            || juego.Rival1Id == jugadorId || juego.Rival2Id == jugadorId;
    }

    private static JuegoRevisionDto Mapear(JuegoRevision r) => new()
    {
        Id = r.Id,
        JuegoPendienteId = r.JuegoPendienteId,
        JuegoDoblesPendienteId = r.JuegoDoblesPendienteId,
        Comentario = r.Comentario,
        Estado = r.Estado.ToString(),
        RespuestaAdmin = r.RespuestaAdmin,
        CreadoEl = r.CreadoEl,
        ResueltoEl = r.ResueltoEl,
    };
}
