using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Avisos in-app. Sin lógica de negocio propia (CRUD fino) → sin test-first,
/// ADR-0005. La usan otros módulos (ranking, y a futuro pedidos/solicitudes)
/// para avisarle a un usuario que tiene algo pendiente.
/// </summary>
public interface INotificacionService
{
    Task NotificarAsync(Guid destinatarioUserId, string tipo, string mensaje, Guid? entidadId = null, CancellationToken ct = default);
    Task<IReadOnlyList<NotificacionDto>> MisAsync(Guid userId, CancellationToken ct = default);
    Task<int> ContarNoLeidasAsync(Guid userId, CancellationToken ct = default);
    Task MarcarTodasLeidasAsync(Guid userId, CancellationToken ct = default);
}

public class NotificacionService : INotificacionService
{
    private const int Top = 50;

    private readonly INotificacionRepository _notificaciones;

    public NotificacionService(INotificacionRepository notificaciones)
    {
        _notificaciones = notificaciones;
    }

    public async Task NotificarAsync(
        Guid destinatarioUserId, string tipo, string mensaje, Guid? entidadId = null, CancellationToken ct = default)
    {
        await _notificaciones.AgregarAsync(new Notificacion
        {
            DestinatarioUserId = destinatarioUserId,
            Tipo = tipo,
            Mensaje = mensaje,
            EntidadId = entidadId,
        }, ct);
        await _notificaciones.GuardarCambiosAsync(ct);
    }

    public async Task<IReadOnlyList<NotificacionDto>> MisAsync(Guid userId, CancellationToken ct = default)
    {
        var mias = await _notificaciones.MisAsync(userId, Top, ct);
        return mias.Select(Mapear).ToList();
    }

    public Task<int> ContarNoLeidasAsync(Guid userId, CancellationToken ct = default) =>
        _notificaciones.ContarNoLeidasAsync(userId, ct);

    public Task MarcarTodasLeidasAsync(Guid userId, CancellationToken ct = default) =>
        _notificaciones.MarcarTodasLeidasAsync(userId, ct);

    private static NotificacionDto Mapear(Notificacion n) => new()
    {
        Id = n.Id,
        Tipo = n.Tipo,
        Mensaje = n.Mensaje,
        EntidadId = n.EntidadId,
        Leida = n.Leida,
        CreadaEl = n.CreadaEl,
    };
}
