using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Repositories;

public class HorarioRepository : IHorarioRepository
{
    private readonly AppDbContext _db;

    private static Guid TenantId => AppDbContext.TenantDemoId;

    public HorarioRepository(AppDbContext db)
    {
        _db = db;
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
            .Include(h => h.Grupo)
            .Include(h => h.Alumno)
            .Where(h => h.TenantId == TenantId && h.Activo)
            .OrderBy(h => h.Dia).ThenBy(h => h.HoraInicio)
            .ToListAsync(ct);

    public Task<Horario?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Horarios.FirstOrDefaultAsync(h => h.TenantId == TenantId && h.Id == id, ct);

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
