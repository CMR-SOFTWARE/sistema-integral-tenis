using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class SueldoService : ISueldoService
{
    private readonly IPoliticaDeSueldo _politica;
    private readonly IPagoEmpleadoRepository _pagos;
    private readonly IMembresiaTenantRepository _membresias;

    public SueldoService(
        IPoliticaDeSueldo politica, IPagoEmpleadoRepository pagos, IMembresiaTenantRepository membresias)
    {
        _politica = politica;
        _pagos = pagos;
        _membresias = membresias;
    }

    public async Task<LiquidacionSueldosDto> ObtenerMesAsync(int anio, int mes, CancellationToken ct = default)
    {
        // Costura VOLÁTIL: la política dice lo que ganó cada empleado (hoy: valor
        // hora × horas). De acá para abajo solo cruzamos con lo YA pagado.
        var calculados = await _politica.CalcularDelMesAsync(anio, mes, ct);
        var pagos = (await _pagos.ListarDelMesAsync(anio, mes, ct)).ToDictionary(p => p.UserId);

        var empleados = calculados
            .Select(c =>
            {
                pagos.TryGetValue(c.UserId, out var pago);
                var pagado = pago?.Monto ?? 0m;
                return new EmpleadoSueldoDto
                {
                    UserId = c.UserId,
                    MembresiaId = c.MembresiaId,
                    Nombre = c.Nombre,
                    Apellido = c.Apellido,
                    Activo = c.Activo,
                    Calculado = c.Monto,
                    Pagado = pagado,
                    Saldo = c.Monto - pagado,
                    Estado = pago is not null ? "Pagado" : "Pendiente",
                    HorasTotales = c.HorasTotales,
                    TieneValorHora = c.TieneValorHora,
                    MedioPago = pago?.MedioPago.ToString(),
                    PagadoEl = pago?.PagadoEl,
                    Detalle = c.Detalle,
                };
            })
            .OrderBy(e => e.Apellido).ThenBy(e => e.Nombre)
            .ToList();

        return new LiquidacionSueldosDto
        {
            Anio = anio,
            Mes = mes,
            TotalAPagar = empleados.Sum(e => e.Calculado),
            TotalPagado = empleados.Sum(e => e.Pagado),
            TotalPendiente = empleados.Sum(e => e.Saldo),
            Empleados = empleados,
        };
    }

    public async Task PagarAsync(
        Guid userId, int anio, int mes, decimal monto, MedioPago medio, CancellationToken ct = default)
    {
        if (monto < 0)
            throw new ReglaDeNegocioException("El monto no puede ser negativo.");

        // El profe tiene que ser de tu equipo (defensa extra al scoping por tenant).
        if (await _membresias.ObtenerPorUserIdAsync(userId, ct) is null)
            throw new ReglaDeNegocioException("Ese profe no está en tu equipo.");

        // Uno por (empleado, mes): si ya se pagó, se revierte antes de corregir.
        if (await _pagos.ObtenerDelMesAsync(userId, anio, mes, ct) is not null)
            throw new ReglaDeNegocioException("Ese mes ya está pagado. Revertí el pago si querés corregirlo.");

        var pago = new PagoEmpleado
        {
            UserId = userId,
            Anio = anio,
            Mes = mes,
            Monto = monto,
            MedioPago = medio,
            PagadoEl = DateTime.UtcNow, // la fecha la pone el server, nunca el cliente
        };
        await _pagos.AgregarAsync(pago, ct);
        await _pagos.GuardarCambiosAsync(ct);
    }

    public async Task RevertirPagoAsync(Guid userId, int anio, int mes, CancellationToken ct = default)
    {
        var pago = await _pagos.ObtenerDelMesAsync(userId, anio, mes, ct)
            ?? throw new ReglaDeNegocioException("No hay un pago registrado ese mes.");

        _pagos.Eliminar(pago);
        await _pagos.GuardarCambiosAsync(ct);
    }

    public async Task<IReadOnlyList<SueldoMesDto>> ObtenerReporteAsync(int meses, CancellationToken ct = default)
    {
        if (meses < 1) meses = 6;
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var desde = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(-(meses - 1));

        var porMes = await _pagos.SumarPorMesAsync(desde.Year, desde.Month, hoy.Year, hoy.Month, ct);

        return Enumerable.Range(0, meses)
            .Select(i => desde.AddMonths(i))
            .Select(f => new SueldoMesDto
            {
                Anio = f.Year,
                Mes = f.Month,
                Pagado = porMes.TryGetValue((f.Year, f.Month), out var t) ? t : 0m,
            })
            .ToList();
    }
}
