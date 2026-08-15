using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

/// <summary>
/// Acceso CROSS-TENANT intencional (ranking global): no inyecta ITenantActual.
/// Mismo criterio que JugadorRankingRepository/NotificacionRepository.
/// </summary>
public interface IJuegoPendienteRepository
{
    Task AgregarAsync(JuegoPendiente juego, CancellationToken ct = default);
    Task<JuegoPendiente?> ObtenerAsync(Guid id, CancellationToken ct = default);

    /// <summary>¿Ya existe CUALQUIER desafío (incluido Finalizado) entre este par normalizado?</summary>
    Task<bool> ExisteEntreAsync(Guid jugadorMenorId, Guid jugadorMayorId, CancellationToken ct = default);

    /// <summary>Los Propuestos/Aceptados donde participa este jugador.</summary>
    Task<IReadOnlyList<JuegoPendiente>> MisPendientesAsync(Guid jugadorId, CancellationToken ct = default);

    /// <summary>Los Finalizados donde participa este jugador — para pedir revisión. Más recientes primero, tope 30.</summary>
    Task<IReadOnlyList<JuegoPendiente>> MisFinalizadosAsync(Guid jugadorId, CancellationToken ct = default);

    /// <summary>¿Tiene ya un partido Propuesto o Aceptado (como cualquiera de los dos lados)? Un jugador solo puede tener uno activo a la vez.</summary>
    Task<bool> TieneActivoAsync(Guid jugadorId, CancellationToken ct = default);

    void Eliminar(JuegoPendiente juego);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class JuegoPendienteRepository : IJuegoPendienteRepository
{
    private readonly AppDbContext _db;

    public JuegoPendienteRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AgregarAsync(JuegoPendiente juego, CancellationToken ct = default)
    {
        _db.JuegosPendientes.Add(juego);
        await Task.CompletedTask;
    }

    public Task<JuegoPendiente?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.JuegosPendientes.FirstOrDefaultAsync(j => j.Id == id, ct);

    public Task<bool> ExisteEntreAsync(Guid jugadorMenorId, Guid jugadorMayorId, CancellationToken ct = default) =>
        _db.JuegosPendientes.AnyAsync(
            j => j.JugadorMenorId == jugadorMenorId && j.JugadorMayorId == jugadorMayorId, ct);

    public async Task<IReadOnlyList<JuegoPendiente>> MisPendientesAsync(Guid jugadorId, CancellationToken ct = default) =>
        await _db.JuegosPendientes
            .Where(j => (j.Jugador1Id == jugadorId || j.Jugador2Id == jugadorId)
                && j.Estado != EstadoJuegoPendiente.Finalizado)
            .OrderByDescending(j => j.CreadoEl)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<JuegoPendiente>> MisFinalizadosAsync(Guid jugadorId, CancellationToken ct = default) =>
        await _db.JuegosPendientes
            .Where(j => (j.Jugador1Id == jugadorId || j.Jugador2Id == jugadorId)
                && j.Estado == EstadoJuegoPendiente.Finalizado)
            .OrderByDescending(j => j.FinalizadoEn)
            .Take(30)
            .ToListAsync(ct);

    public Task<bool> TieneActivoAsync(Guid jugadorId, CancellationToken ct = default) =>
        _db.JuegosPendientes.AnyAsync(
            j => (j.Jugador1Id == jugadorId || j.Jugador2Id == jugadorId) && j.Estado != EstadoJuegoPendiente.Finalizado, ct);

    public void Eliminar(JuegoPendiente juego) => _db.JuegosPendientes.Remove(juego);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
