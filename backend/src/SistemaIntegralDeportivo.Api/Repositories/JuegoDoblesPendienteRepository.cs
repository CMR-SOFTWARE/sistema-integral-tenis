using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>
/// Acceso CROSS-TENANT intencional (ranking global de dobles): no inyecta
/// ITenantActual. A diferencia de singles, acá NO hay índice único en base
/// para el bloqueo de pareja-vs-pareja (normalizar 4 Guid es enrevesado) —
/// se valida en DesafioDoblesService con ExisteActivoEntreParejasAsync.
/// </summary>
public interface IJuegoDoblesPendienteRepository
{
    Task AgregarAsync(JuegoDoblesPendiente juego, CancellationToken ct = default);
    Task<JuegoDoblesPendiente?> ObtenerAsync(Guid id, CancellationToken ct = default);

    /// <summary>¿Ya hay un desafío Propuesto/Aceptado entre EXACTAMENTE estas dos parejas
    /// (orden de compañero/rival indiferente)? Finalizado NO bloquea: revancha permitida.</summary>
    Task<bool> ExisteActivoEntreParejasAsync(
        Guid jugador1Id, Guid jugador2Id, Guid rival1Id, Guid rival2Id, CancellationToken ct = default);

    /// <summary>¿Este jugador ya participa (en cualquiera de las 4 posiciones) de un
    /// desafío Propuesto/Aceptado? Un jugador solo tiene un partido de DOBLES activo a la vez.</summary>
    Task<bool> TieneActivoAsync(Guid jugadorId, CancellationToken ct = default);

    /// <summary>Los Propuestos/Aceptados donde participa este jugador (en cualquier posición).</summary>
    Task<IReadOnlyList<JuegoDoblesPendiente>> MisPendientesAsync(Guid jugadorId, CancellationToken ct = default);

    /// <summary>Los Finalizados donde participa este jugador — para pedir revisión. Más recientes primero, tope 30.</summary>
    Task<IReadOnlyList<JuegoDoblesPendiente>> MisFinalizadosAsync(Guid jugadorId, CancellationToken ct = default);

    void Eliminar(JuegoDoblesPendiente juego);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class JuegoDoblesPendienteRepository : IJuegoDoblesPendienteRepository
{
    private readonly AppDbContext _db;

    public JuegoDoblesPendienteRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AgregarAsync(JuegoDoblesPendiente juego, CancellationToken ct = default)
    {
        _db.JuegosDoblesPendientes.Add(juego);
        await Task.CompletedTask;
    }

    public Task<JuegoDoblesPendiente?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.JuegosDoblesPendientes.FirstOrDefaultAsync(j => j.Id == id, ct);

    public Task<bool> ExisteActivoEntreParejasAsync(
        Guid jugador1Id, Guid jugador2Id, Guid rival1Id, Guid rival2Id, CancellationToken ct = default) =>
        _db.JuegosDoblesPendientes.AnyAsync(j =>
            j.Estado != EstadoJuegoPendiente.Finalizado &&
            ((j.Jugador1Id == jugador1Id && j.Jugador2Id == jugador2Id) ||
             (j.Jugador1Id == jugador2Id && j.Jugador2Id == jugador1Id)) &&
            ((j.Rival1Id == rival1Id && j.Rival2Id == rival2Id) ||
             (j.Rival1Id == rival2Id && j.Rival2Id == rival1Id)), ct);

    public Task<bool> TieneActivoAsync(Guid jugadorId, CancellationToken ct = default) =>
        _db.JuegosDoblesPendientes.AnyAsync(j =>
            (j.Jugador1Id == jugadorId || j.Jugador2Id == jugadorId ||
             j.Rival1Id == jugadorId || j.Rival2Id == jugadorId) &&
            j.Estado != EstadoJuegoPendiente.Finalizado, ct);

    public async Task<IReadOnlyList<JuegoDoblesPendiente>> MisPendientesAsync(Guid jugadorId, CancellationToken ct = default) =>
        await _db.JuegosDoblesPendientes
            .Where(j => (j.Jugador1Id == jugadorId || j.Jugador2Id == jugadorId ||
                         j.Rival1Id == jugadorId || j.Rival2Id == jugadorId)
                && j.Estado != EstadoJuegoPendiente.Finalizado)
            .OrderByDescending(j => j.CreadoEl)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<JuegoDoblesPendiente>> MisFinalizadosAsync(Guid jugadorId, CancellationToken ct = default) =>
        await _db.JuegosDoblesPendientes
            .Where(j => (j.Jugador1Id == jugadorId || j.Jugador2Id == jugadorId ||
                         j.Rival1Id == jugadorId || j.Rival2Id == jugadorId)
                && j.Estado == EstadoJuegoPendiente.Finalizado)
            .OrderByDescending(j => j.FinalizadoEn)
            .Take(30)
            .ToListAsync(ct);

    public void Eliminar(JuegoDoblesPendiente juego) => _db.JuegosDoblesPendientes.Remove(juego);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
