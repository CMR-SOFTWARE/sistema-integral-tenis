using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class CuotaService : ICuotaService
{
    /// <summary>Día del mes en que vence la liquidación (modelo-precios.md).</summary>
    public const int DiaVencimiento = 10;

    /// <summary>
    /// Día del mes a partir del cual el profe puede sacar del calendario al
    /// que no pagó (5 días de gracia después del vencimiento). No es
    /// automático: se le sugiere al profe y él confirma.
    /// </summary>
    public const int DiaSuspension = 15;

    private readonly ICargoRepository _cargos;
    private readonly IAlumnoRepository _alumnos;
    private readonly IPoliticaDeCuota _politica;

    public CuotaService(
        ICargoRepository cargos, IAlumnoRepository alumnos, IPoliticaDeCuota politica)
    {
        _cargos = cargos;
        _alumnos = alumnos;
        _politica = politica;
    }

    public async Task<LiquidacionMesDto> ObtenerMesAsync(int anio, int mes, CancellationToken ct = default)
    {
        // Costura VOLÁTIL: la política asegura los cargos del mes (hoy: cuota
        // mensual manual). De acá para abajo solo LEEMOS el ledger de cargos, así
        // que cambiar el modelo de cobro no toca nada de esto (ver IPoliticaDeCuota).
        await _politica.GenerarCargosDelMesAsync(anio, mes, ct);

        var cargosMes = await _cargos.ListarDelMesAsync(anio, mes, ct);
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var esMesActual = anio == hoy.Year && mes == hoy.Month;

        // Roster a mostrar: los que tienen movimientos este mes + (solo el mes
        // corriente) los alumnos activos CON CLASE asignada (los inscriptos), para
        // ver también a los que están "sin cuota definida". El que no toma clases no
        // aparece (su arancel queda de referencia hasta que tenga una clase).
        var porAlumno = cargosMes.GroupBy(c => c.AlumnoId).ToDictionary(g => g.Key, g => g.ToList());
        var alumnos = new Dictionary<Guid, Alumno>();
        foreach (var c in cargosMes)
            if (c.Alumno is not null) alumnos.TryAdd(c.AlumnoId, c.Alumno);
        if (esMesActual)
        {
            var conClase = await _alumnos.ListarConClaseAsync(ct);
            foreach (var a in await _alumnos.ListarAsync(null, EstadoAlumno.Activo, ct))
                if (conClase.Contains(a.Id)) alumnos.TryAdd(a.Id, a);
        }

        var liquidaciones = alumnos.Values
            .Select(alumno =>
            {
                var cargos = porAlumno.TryGetValue(alumno.Id, out var cs) ? cs : [];
                var total = cargos.Sum(c => c.Monto);
                var pagado = cargos.Where(c => c.PagadoEl is not null).Sum(c => c.Monto);
                var saldo = total - pagado;

                // "Informado": debe, pero avisó que transfirió TODO el saldo y
                // espera que el profe confirme (tapa a "Vencida").
                var impagos = cargos.Where(c => c.PagadoEl is null).ToList();
                var todoInformado = impagos.Count > 0 && impagos.All(c => c.PagoInformadoEl is not null);

                return new AlumnoLiquidacionDto
                {
                    AlumnoId = alumno.Id,
                    Nombre = alumno.Nombre,
                    Apellido = alumno.Apellido,
                    FamiliaId = alumno.UserId,
                    Modalidad = alumno.Modalidad.ToString(),
                    Total = total,
                    Pagado = pagado,
                    Saldo = saldo,
                    Estado = todoInformado ? "Informado" : CalcularEstado(anio, mes, saldo, hoy),
                    // Cuota sin definir: activo sin arancel y sin cargo Cuota → el
                    // profe lo ve marcado para cargarle el valor mensual.
                    CuotaDefinida = alumno.Arancel is > 0 || cargos.Any(c => c.Tipo == TipoCargo.Cuota),
                    Cargos = cargos.OrderBy(c => c.Fecha).ThenBy(c => c.CreadoEl).Select(Mapear).ToList(),
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

    public async Task<CargoResponseDto> EditarMontoCargoAsync(
        Guid cargoId, decimal monto, CancellationToken ct = default)
    {
        var cargo = await _cargos.ObtenerAsync(cargoId, ct)
            ?? throw new ReglaDeNegocioException("El cargo no existe.");

        // Editar el monto al cobrar (ej. ajustar la cuota de un alumno ese mes).
        // Lo pagado es historia: no se retoca.
        if (cargo.PagadoEl is not null)
            throw new ReglaDeNegocioException("Ese cargo ya está pagado: no se puede cambiar el monto.");
        if (monto < 0)
            throw new ReglaDeNegocioException("El monto no puede ser negativo.");

        cargo.Monto = monto;
        await _cargos.GuardarCambiosAsync(ct);
        return Mapear(cargo);
    }

    public async Task<IReadOnlyList<RecaudadoMesDto>> ObtenerReporteAsync(
        int meses, CancellationToken ct = default)
    {
        if (meses < 1) meses = 6;
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var desde = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(-(meses - 1));
        var hasta = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(1).AddDays(-1);

        var porMes = await _cargos.SumarPagadosPorMesAsync(desde, hasta, ct);

        return Enumerable.Range(0, meses)
            .Select(i => desde.AddMonths(i))
            .Select(f => new RecaudadoMesDto
            {
                Anio = f.Year,
                Mes = f.Month,
                Recaudado = porMes.TryGetValue((f.Year, f.Month), out var t) ? t : 0m,
            })
            .ToList();
    }

    public async Task<CargoResponseDto> AgregarCargoManualAsync(
        CreateCargoManualDto dto, CancellationToken ct = default)
    {
        if (dto.Tipo is TipoCargo.Clase or TipoCargo.Cuota)
            throw new ReglaDeNegocioException(
                "Solo se cargan a mano Producto o Ajuste (la cuota mensual se genera sola).");

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

    // ── Pago informado (portal): el alumno AVISA que transfirió; el profe
    //    confirma (PagarMes/PagarCargo) o rechaza. La plata no se da por
    //    cobrada hasta la confirmación — la verdad la pone el profe. ──

    public async Task InformarPagoMesAsync(
        Guid alumnoId, int anio, int mes, CancellationToken ct = default)
    {
        var cargosMes = await _cargos.ListarDelMesAsync(anio, mes, ct);
        var aInformar = cargosMes
            .Where(c => c.AlumnoId == alumnoId && c.PagadoEl is null && c.PagoInformadoEl is null)
            .ToList();

        if (aInformar.Count == 0)
            throw new ReglaDeNegocioException("No tenés cargos pendientes por informar en ese mes.");

        var ahora = DateTime.UtcNow; // la fecha la pone el server, nunca el cliente
        foreach (var cargo in aInformar)
            cargo.PagoInformadoEl = ahora;

        await _cargos.GuardarCambiosAsync(ct);
    }

    public async Task InformarPagoCargoAsync(
        Guid alumnoId, Guid cargoId, CancellationToken ct = default)
    {
        var cargo = await _cargos.ObtenerAsync(cargoId, ct)
            ?? throw new ReglaDeNegocioException("El cargo no existe.");

        // El cargo tiene que ser MÍO (defensa extra al scoping por tenant)
        if (cargo.AlumnoId != alumnoId)
            throw new ReglaDeNegocioException("Ese cargo no es tuyo.");
        if (cargo.PagadoEl is not null)
            throw new ReglaDeNegocioException("Ese cargo ya está pagado.");
        if (cargo.PagoInformadoEl is not null)
            throw new ReglaDeNegocioException("Ya informaste el pago de ese cargo.");

        cargo.PagoInformadoEl = DateTime.UtcNow;
        await _cargos.GuardarCambiosAsync(ct);
    }

    public async Task RechazarPagoMesAsync(
        Guid alumnoId, int anio, int mes, CancellationToken ct = default)
    {
        var cargosMes = await _cargos.ListarDelMesAsync(anio, mes, ct);
        var informados = cargosMes
            .Where(c => c.AlumnoId == alumnoId && c.PagadoEl is null && c.PagoInformadoEl is not null)
            .ToList();

        if (informados.Count == 0)
            throw new ReglaDeNegocioException("No hay pagos informados sin confirmar en ese mes.");

        // "No me llegó": vuelve a impago sin informar (el alumno puede reintentar)
        foreach (var cargo in informados)
            cargo.PagoInformadoEl = null;

        await _cargos.GuardarCambiosAsync(ct);
    }

    public async Task RechazarPagoCargoAsync(Guid cargoId, CancellationToken ct = default)
    {
        var cargo = await _cargos.ObtenerAsync(cargoId, ct)
            ?? throw new ReglaDeNegocioException("El cargo no existe.");

        if (cargo.PagadoEl is not null)
            throw new ReglaDeNegocioException("Ese cargo ya está pagado.");
        if (cargo.PagoInformadoEl is null)
            throw new ReglaDeNegocioException("Ese cargo no tiene un pago informado.");

        cargo.PagoInformadoEl = null;
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
    /// Morosidad DURA: algún cargo impago de un mes cuyo día 15 ya pasó. El
    /// profe puede sacarlo del calendario (pausarlo) desde el panel de
    /// morosos — nunca es automático.
    /// </summary>
    public static bool DebeSuspenderse(IEnumerable<Cargo> cargosImpagos, DateOnly hoy) =>
        cargosImpagos.Any(c => hoy > new DateOnly(c.Fecha.Year, c.Fecha.Month, DiaSuspension));

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
        PagoInformado = c.PagoInformadoEl is not null && c.PagadoEl is null,
        PagoInformadoEl = c.PagoInformadoEl,
    };
}
