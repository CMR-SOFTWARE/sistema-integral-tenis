using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public class HorarioRepository : IHorarioRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;

    // El tenant sale del token o del override del portal (ADR-0010)
    private Guid TenantId => _tenantActual.TenantId;

    public HorarioRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task<IReadOnlyList<Horario>> ListarPorCanchaYDiaAsync(
        Guid canchaId, DayOfWeek dia, CancellationToken ct = default) =>
        await _db.Horarios
            .AsNoTracking()
            .Where(h => h.TenantId == TenantId && h.CanchaId == canchaId && h.Dia == dia && h.Activo)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Horario>> ListarActivosAsync(CancellationToken ct = default) =>
        await _db.Horarios
            .AsNoTracking()
            .Include(h => h.Cancha).ThenInclude(c => c.Sede)
            .Include(h => h.Alumnos).ThenInclude(ah => ah.Alumno)
            .Where(h => h.TenantId == TenantId && h.Activo)
            .OrderBy(h => h.Dia).ThenBy(h => h.HoraInicio)
            .ToListAsync(ct);

    public Task<Horario?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Horarios.FirstOrDefaultAsync(h => h.TenantId == TenantId && h.Id == id, ct);

    public Task<Horario?> ObtenerConRosterAsync(Guid id, CancellationToken ct = default) =>
        _db.Horarios
            .Include(h => h.Cancha).ThenInclude(c => c.Sede)
            .Include(h => h.Alumnos).ThenInclude(ah => ah.Alumno)
            .FirstOrDefaultAsync(h => h.TenantId == TenantId && h.Id == id, ct);

    // ── El roster ──

    public Task<AlumnoHorario?> ObtenerMembresiaAsync(Guid horarioId, Guid alumnoId, CancellationToken ct = default) =>
        _db.AlumnoHorarios.FirstOrDefaultAsync(ah => ah.HorarioId == horarioId && ah.AlumnoId == alumnoId, ct);

    public async Task AgregarMembresiaAsync(AlumnoHorario membresia, CancellationToken ct = default)
    {
        _db.AlumnoHorarios.Add(membresia);
        await Task.CompletedTask; // se persiste con GuardarCambiosAsync
    }

    public Task<int> ContarActivosAsync(Guid horarioId, CancellationToken ct = default) =>
        _db.AlumnoHorarios.CountAsync(ah => ah.HorarioId == horarioId && ah.FechaBaja == null, ct);

    public async Task<IReadOnlyList<AlumnoHorario>> ListarMembresiasActivasDeAlumnoAsync(
        Guid alumnoId, CancellationToken ct = default) =>
        // TRACKEADO: dar de baja al alumno cierra estas membresías
        await _db.AlumnoHorarios
            .Where(ah => ah.AlumnoId == alumnoId && ah.FechaBaja == null && ah.Horario.TenantId == TenantId)
            .ToListAsync(ct);

    public async Task<Horario> AgregarAsync(Horario horario, CancellationToken ct = default)
    {
        horario.TenantId = TenantId;
        _db.Horarios.Add(horario);
        await _db.SaveChangesAsync(ct);
        return horario;
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
