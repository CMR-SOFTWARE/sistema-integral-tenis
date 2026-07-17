using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public class TurnoRepository : ITurnoRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;

    // El tenant sale del token o del override del portal (ADR-0010)
    private Guid TenantId => _tenantActual.TenantId;

    public TurnoRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task<IReadOnlyList<Turno>> ListarEntreAsync(
        DateOnly desde, DateOnly hasta, CancellationToken ct = default) =>
        await _db.Turnos
            .AsNoTracking()
            .Include(t => t.Cancha).ThenInclude(c => c.Sede)
            .Include(t => t.Horario).ThenInclude(h => h.Grupo)
            .Include(t => t.Horario).ThenInclude(h => h.Alumno)
            .Include(t => t.Participantes).ThenInclude(p => p.Alumno)
            .Where(t => t.TenantId == TenantId && t.Fecha >= desde && t.Fecha <= hasta)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DateOnly>> FechasGeneradasAsync(
        Guid horarioId, DateOnly desde, DateOnly hasta, CancellationToken ct = default) =>
        await _db.Turnos
            .Where(t => t.HorarioId == horarioId && t.Fecha >= desde && t.Fecha <= hasta)
            .Select(t => t.Fecha)
            .ToListAsync(ct);

    public Task<Turno?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Turnos
            .Include(t => t.Participantes)
            .FirstOrDefaultAsync(t => t.TenantId == TenantId && t.Id == id, ct);

    public async Task<IReadOnlyList<Turno>> ListarPorAlumnoEntreAsync(
        Guid alumnoId, DateOnly desde, DateOnly hasta, CancellationToken ct = default) =>
        await _db.Turnos
            .AsNoTracking()
            .Include(t => t.Cancha).ThenInclude(c => c.Sede)
            .Include(t => t.Horario).ThenInclude(h => h.Grupo)
            .Include(t => t.Participantes).ThenInclude(p => p.Alumno) // compañeros
            .Where(t => t.Fecha >= desde && t.Fecha <= hasta &&
                        t.Participantes.Any(p => p.AlumnoId == alumnoId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Turno>> ListarCanceladosRecientesAsync(
        int cantidad, CancellationToken ct = default) =>
        await _db.Turnos
            .AsNoTracking()
            .Include(t => t.Cancha)
            .Include(t => t.Horario).ThenInclude(h => h.Grupo)
            .Include(t => t.Horario).ThenInclude(h => h.Alumno)
            .Where(t => t.TenantId == TenantId && t.Estado == EstadoTurno.Cancelado)
            .OrderByDescending(t => t.CanceladoEl)
            .Take(cantidad)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TurnoParticipante>> ListarAvisosRecientesAsync(
        int cantidad, CancellationToken ct = default) =>
        await _db.TurnoParticipantes
            .AsNoTracking()
            .Include(p => p.Alumno)
            .Include(p => p.Turno).ThenInclude(t => t.Horario).ThenInclude(h => h.Grupo)
            .Include(p => p.Turno).ThenInclude(t => t.Horario).ThenInclude(h => h.Alumno)
            .Where(p => p.Turno.TenantId == TenantId && p.CanceloEl != null)
            .OrderByDescending(p => p.CanceloEl)
            .Take(cantidad)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Turno>> ListarProgramadosDesdeAsync(
        DateOnly desde, Guid? canchaId, CancellationToken ct = default) =>
        await _db.Turnos
            .Include(t => t.Cancha)
            .Include(t => t.Horario).ThenInclude(h => h.Grupo)
            .Include(t => t.Horario).ThenInclude(h => h.Alumno)
            .Include(t => t.Participantes).ThenInclude(p => p.Alumno)
            .Where(t => t.TenantId == TenantId && t.Estado == EstadoTurno.Programado &&
                        t.Fecha >= desde &&
                        (canchaId == null || t.CanchaId == canchaId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Turno>> ListarPorHorarioDesdeAsync(
        Guid horarioId, DateOnly desde, CancellationToken ct = default) =>
        await _db.Turnos
            .Include(t => t.Participantes)
            .Where(t => t.TenantId == TenantId && t.HorarioId == horarioId && t.Fecha >= desde)
            .ToListAsync(ct);

    public Task AgregarAsync(Turno turno, CancellationToken ct = default)
    {
        turno.TenantId = TenantId;
        _db.Turnos.Add(turno);
        return Task.CompletedTask; // se persiste con GuardarCambiosAsync (una sola transacción por semana)
    }

    public async Task<IReadOnlyList<Turno>> ListarFuturosDeAlumnoAsync(
        Guid alumnoId, DateOnly desde, CancellationToken ct = default) =>
        // TRACKEADO a propósito (sin AsNoTracking): el caller muta el roster
        await _db.Turnos
            .Include(t => t.Participantes)
            .Where(t => t.TenantId == TenantId &&
                        t.Estado == EstadoTurno.Programado &&
                        t.Fecha >= desde &&
                        t.Participantes.Any(p => p.AlumnoId == alumnoId))
            .ToListAsync(ct);

    public void Eliminar(Turno turno) =>
        _db.Turnos.Remove(turno); // los participantes caen en cascada

    public void QuitarParticipante(TurnoParticipante participante) =>
        _db.TurnoParticipantes.Remove(participante);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
