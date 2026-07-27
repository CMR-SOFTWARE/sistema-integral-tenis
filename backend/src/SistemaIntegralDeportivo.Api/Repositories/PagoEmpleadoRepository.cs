using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public class PagoEmpleadoRepository : IPagoEmpleadoRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;

    // El tenant sale del token o del override del portal (ADR-0010)
    private Guid TenantId => _tenantActual.TenantId;

    public PagoEmpleadoRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public async Task<IReadOnlyList<PagoEmpleado>> ListarDelMesAsync(int anio, int mes, CancellationToken ct = default) =>
        await _db.PagosEmpleado
            .AsNoTracking()
            .Where(p => p.TenantId == TenantId && p.Anio == anio && p.Mes == mes)
            .ToListAsync(ct);

    public Task<PagoEmpleado?> ObtenerDelMesAsync(Guid userId, int anio, int mes, CancellationToken ct = default) =>
        _db.PagosEmpleado.FirstOrDefaultAsync(
            p => p.TenantId == TenantId && p.UserId == userId && p.Anio == anio && p.Mes == mes, ct);

    public async Task<Dictionary<(int Anio, int Mes), decimal>> SumarPorMesAsync(
        int desdeAnio, int desdeMes, int hastaAnio, int hastaMes, CancellationToken ct = default)
    {
        // Clave lineal (año*12 + mes) para comparar períodos sin bailar con el corte de año.
        var desdeKey = desdeAnio * 12 + (desdeMes - 1);
        var hastaKey = hastaAnio * 12 + (hastaMes - 1);

        var porMes = await _db.PagosEmpleado
            .Where(p => p.TenantId == TenantId
                        && p.Anio * 12 + (p.Mes - 1) >= desdeKey
                        && p.Anio * 12 + (p.Mes - 1) <= hastaKey)
            .GroupBy(p => new { p.Anio, p.Mes })
            .Select(g => new { g.Key.Anio, g.Key.Mes, Total = g.Sum(x => x.Monto) })
            .ToListAsync(ct);

        return porMes.ToDictionary(x => (x.Anio, x.Mes), x => x.Total);
    }

    public async Task AgregarAsync(PagoEmpleado pago, CancellationToken ct = default)
    {
        pago.TenantId = TenantId;
        _db.PagosEmpleado.Add(pago);
        await Task.CompletedTask; // se persiste con GuardarCambiosAsync
    }

    public void Eliminar(PagoEmpleado pago) =>
        _db.PagosEmpleado.Remove(pago);

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
