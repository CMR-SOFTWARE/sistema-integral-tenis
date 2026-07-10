using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class CuotaService : ICuotaService
{
    /// <summary>Día del mes en que vence la liquidación (modelo-precios.md).</summary>
    public const int DiaVencimiento = 10;

    private readonly ICargoRepository _cargos;
    private readonly ITurnoRepository _turnos;
    private readonly ITenantRepository _tenant;
    private readonly ITurnoService _turnoService;

    public CuotaService(
        ICargoRepository cargos, ITurnoRepository turnos, ITenantRepository tenant, ITurnoService turnoService)
    {
        _cargos = cargos;
        _turnos = turnos;
        _tenant = tenant;
        _turnoService = turnoService;
    }

    public async Task<LiquidacionMesDto> ObtenerMesAsync(int anio, int mes, CancellationToken ct = default)
    {
        var tenant = await _tenant.ObtenerActualAsync(ct);
        var primerDia = new DateOnly(anio, mes, 1);
        var ultimoDia = primerDia.AddMonths(1).AddDays(-1);

        // Materializar los turnos del mes ANTES de liquidar: la cuota no
        // depende de que se haya paseado por el Calendario semana a semana
        await _turnoService.GenerarTurnosDelMesAsync(anio, mes, ct);

        // ── Generación perezosa de cargos de clase (idempotente) ──
        var turnos = await _turnos.ListarEntreAsync(primerDia, ultimoDia, ct);
        var existentes = await _cargos.ListarDelMesAsync(anio, mes, ct);
        var yaCargados = existentes
            .Where(c => c.TurnoId is not null)
            .Select(c => (Turno: c.TurnoId!.Value, Alumno: c.AlumnoId))
            .ToHashSet();

        var generoAlguno = false;
        foreach (var turno in turnos)
        {
            // Cancelado = la clase no ocurrió → nadie paga
            if (turno.Estado == EstadoTurno.Cancelado) continue;
            if (turno.Participantes.Count == 0) continue;

            // La fórmula (modelo-precios.md): ambos precios son POR HORA y se
            // prorratean por la duración del turno; grupal además se divide
            // entre los ASIGNADOS (el ausente paga igual)
            var esIndividual = turno.Horario?.AlumnoId is not null;
            var factorDuracion = turno.DuracionMinutos / 60m;
            decimal monto;
            string concepto;
            if (esIndividual)
            {
                var valorHora = tenant.ValorClaseIndividual
                    ?? throw new ReglaDeNegocioException(
                        "Configurá el valor hora de la clase individual antes de liquidar (Configuración).");
                monto = Math.Round(valorHora * factorDuracion, 2);
                concepto = $"Clase individual ({turno.DuracionMinutos}')";
            }
            else
            {
                var valorHora = tenant.ValorHoraGrupal
                    ?? throw new ReglaDeNegocioException(
                        "Configurá el valor hora grupal antes de liquidar (Configuración).");
                monto = Math.Round(valorHora * factorDuracion / turno.Participantes.Count, 2);
                concepto = $"Clase grupal — {turno.Horario?.Grupo?.Nombre ?? "grupo"} ({turno.Participantes.Count})";
            }

            foreach (var p in turno.Participantes)
            {
                if (yaCargados.Contains((turno.Id, p.AlumnoId))) continue;

                await _cargos.AgregarAsync(new Cargo
                {
                    AlumnoId = p.AlumnoId,
                    Tipo = TipoCargo.Clase,
                    Concepto = concepto,
                    Monto = monto,          // snapshot: cambios de precio no lo tocan
                    Fecha = turno.Fecha,
                    TurnoId = turno.Id,
                    // TenantId lo asigna el repositorio
                }, ct);
                generoAlguno = true;
            }
        }

        if (generoAlguno)
            await _cargos.GuardarCambiosAsync(ct);

        // ── La liquidación: cargos del mes agrupados por alumno ──
        var cargosMes = await _cargos.ListarDelMesAsync(anio, mes, ct);
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        var liquidaciones = cargosMes
            .GroupBy(c => c.AlumnoId)
            .Select(g =>
            {
                var total = g.Sum(c => c.Monto);
                var pagado = g.Where(c => c.PagadoEl is not null).Sum(c => c.Monto);
                var saldo = total - pagado;
                var alumno = g.Select(c => c.Alumno).FirstOrDefault(a => a is not null);
                return new AlumnoLiquidacionDto
                {
                    AlumnoId = g.Key,
                    Nombre = alumno?.Nombre ?? string.Empty,
                    Apellido = alumno?.Apellido ?? string.Empty,
                    Modalidad = alumno?.Modalidad.ToString() ?? string.Empty,
                    Total = total,
                    Pagado = pagado,
                    Saldo = saldo,
                    Estado = CalcularEstado(anio, mes, saldo, hoy),
                    Cargos = g.OrderBy(c => c.Fecha).ThenBy(c => c.CreadoEl).Select(Mapear).ToList(),
                };
            })
            .OrderBy(l => l.Apellido).ThenBy(l => l.Nombre)
            .ToList();

        return new LiquidacionMesDto
        {
            Anio = anio,
            Mes = mes,
            TotalFacturado = liquidaciones.Sum(l => l.Total),
            TotalCobrado = liquidaciones.Sum(l => l.Pagado),
            TotalPendiente = liquidaciones.Sum(l => l.Saldo),
            AlumnosVencidos = liquidaciones.Count(l => l.Estado == "Vencida"),
            Liquidaciones = liquidaciones,
        };
    }

    public async Task<CargoResponseDto> AgregarCargoManualAsync(
        CreateCargoManualDto dto, CancellationToken ct = default)
    {
        if (dto.Tipo == TipoCargo.Clase)
            throw new ReglaDeNegocioException(
                "Los cargos de clase se generan automáticamente desde los turnos.");

        var cargo = new Cargo
        {
            AlumnoId = dto.AlumnoId,
            Tipo = dto.Tipo,
            Concepto = dto.Concepto,
            Monto = dto.Monto,
            Fecha = dto.Fecha ?? DateOnly.FromDateTime(DateTime.UtcNow),
        };

        await _cargos.AgregarAsync(cargo, ct);
        await _cargos.GuardarCambiosAsync(ct);
        return Mapear(cargo);
    }

    public async Task PagarMesAsync(
        Guid alumnoId, int anio, int mes, MedioPago medio, CancellationToken ct = default)
    {
        var cargosMes = await _cargos.ListarDelMesAsync(anio, mes, ct);
        var impagos = cargosMes
            .Where(c => c.AlumnoId == alumnoId && c.PagadoEl is null)
            .ToList();

        if (impagos.Count == 0)
            throw new ReglaDeNegocioException("El alumno no tiene cargos impagos en ese mes.");

        var ahora = DateTime.UtcNow; // la fecha la pone el server, nunca el cliente
        foreach (var cargo in impagos)
        {
            cargo.PagadoEl = ahora;
            cargo.MedioPago = medio;
        }

        await _cargos.GuardarCambiosAsync(ct);
    }

    public async Task PagarCargoAsync(Guid cargoId, MedioPago medio, CancellationToken ct = default)
    {
        var cargo = await _cargos.ObtenerAsync(cargoId, ct)
            ?? throw new ReglaDeNegocioException("El cargo no existe.");

        if (cargo.PagadoEl is not null)
            throw new ReglaDeNegocioException("El cargo ya está pagado.");

        cargo.PagadoEl = DateTime.UtcNow;
        cargo.MedioPago = medio;
        await _cargos.GuardarCambiosAsync(ct);
    }

    /// <summary>
    /// Morosidad: true si algún cargo impago pertenece a un mes cuya
    /// liquidación ya venció (día 10). Bloquea asignaciones NUEVAS (grupo u
    /// horario individual); las clases ya asignadas siguen y se cobran igual.
    /// </summary>
    public static bool TieneDeudaVencida(IEnumerable<Cargo> cargosImpagos, DateOnly hoy) =>
        cargosImpagos.Any(c => CalcularEstado(c.Fecha.Year, c.Fecha.Month, saldo: 1m, hoy) == "Vencida");

    /// <summary>
    /// Estado de la liquidación, CALCULADO (nunca guardado): Pagada si no
    /// debe nada; si debe, Vencida cuando "hoy" pasó el día 10 de ese mes.
    /// </summary>
    public static string CalcularEstado(int anio, int mes, decimal saldo, DateOnly hoy)
    {
        if (saldo <= 0) return "Pagada";
        var vencimiento = new DateOnly(anio, mes, DiaVencimiento);
        return hoy > vencimiento ? "Vencida" : "Pendiente";
    }

    private static CargoResponseDto Mapear(Cargo c) => new()
    {
        Id = c.Id,
        Tipo = c.Tipo.ToString(),
        Concepto = c.Concepto,
        Monto = c.Monto,
        Fecha = c.Fecha,
        Pagado = c.PagadoEl is not null,
        PagadoEl = c.PagadoEl,
        MedioPago = c.MedioPago?.ToString(),
    };
}
