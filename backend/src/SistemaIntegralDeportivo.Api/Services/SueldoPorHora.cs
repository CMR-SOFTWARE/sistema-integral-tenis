using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Política de sueldo de HOY: valor hora × horas dadas. El valor hora sale del
/// horario (override para el caso "menores") o, si no tiene, del valor hora base
/// del empleado. Solo cuentan las clases DADAS (turnos Programado): la cancelada
/// no se paga. El dueño no es empleado (no está en las membresías), así que sus
/// clases no generan sueldo.
/// </summary>
public class SueldoPorHora : IPoliticaDeSueldo
{
    private readonly ITurnoService _turnoService;
    private readonly ITurnoRepository _turnos;
    private readonly IMembresiaTenantRepository _membresias;

    public SueldoPorHora(
        ITurnoService turnoService, ITurnoRepository turnos, IMembresiaTenantRepository membresias)
    {
        _turnoService = turnoService;
        _turnos = turnos;
        _membresias = membresias;
    }

    public async Task<IReadOnlyList<SueldoCalculadoDto>> CalcularDelMesAsync(
        int anio, int mes, CancellationToken ct = default)
    {
        // Asegurar que el mes esté materializado (misma costura que la cuota: no
        // dependemos de que se haya visitado el Calendario).
        await _turnoService.GenerarTurnosDelMesAsync(anio, mes, ct);

        var primerDia = new DateOnly(anio, mes, 1);
        var ultimoDia = primerDia.AddMonths(1).AddDays(-1);
        var turnos = await _turnos.ListarEntreAsync(primerDia, ultimoDia, ct);

        // Solo clases DADAS que cuelgan de un horario CON profe (las sueltas no
        // tienen horario → no son de nadie a la hora de liquidar).
        var porProfe = turnos
            .Where(t => t.Estado == EstadoTurno.Programado && t.Horario?.ProfesorUserId is not null)
            .ToLookup(t => t.Horario!.ProfesorUserId!.Value);

        var empleados = await _membresias.ListarConUsuarioAsync(ct);
        var resultado = new List<SueldoCalculadoDto>();

        foreach (var (m, u) in empleados)
        {
            var susTurnos = porProfe[m.UserId].ToList();

            // Ex-empleado sin clases este mes: no ensucia la lista.
            if (!m.Activo && susTurnos.Count == 0) continue;

            // Desglose por horario: valor hora efectivo × horas de sus clases del mes.
            var detalle = susTurnos
                .GroupBy(t => t.HorarioId!.Value)
                .Select(g =>
                {
                    var horario = g.First().Horario!;
                    var valor = horario.ValorHoraProfe ?? m.ValorHora; // override o base
                    var horas = g.Sum(t => t.DuracionMinutos) / 60m;    // /60m → decimal, no entero
                    return new SueldoHorarioDto
                    {
                        HorarioId = g.Key,
                        Titulo = Titulo(horario),
                        Dia = horario.Dia.ToString(),
                        HoraInicio = horario.HoraInicio,
                        ValorHora = valor,
                        Clases = g.Count(),
                        Horas = horas,
                        Subtotal = (valor ?? 0m) * horas,
                    };
                })
                .OrderBy(d => d.Dia).ThenBy(d => d.HoraInicio)
                .ToList();

            // "Sin valor hora": si dio clases, que alguna quedó sin tarifa; si no
            // dio clases, que todavía no tiene un valor base cargado.
            var tieneValorHora = susTurnos.Count > 0
                ? detalle.All(d => d.ValorHora is not null)
                : m.ValorHora is not null;

            resultado.Add(new SueldoCalculadoDto
            {
                UserId = m.UserId,
                MembresiaId = m.Id,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Activo = m.Activo,
                Monto = detalle.Sum(d => d.Subtotal),
                HorasTotales = detalle.Sum(d => d.Horas),
                TieneValorHora = tieneValorHora,
                Detalle = detalle,
            });
        }

        return resultado;
    }

    private static string Titulo(Horario h) =>
        h.Grupo?.Nombre
        ?? (h.Alumno is not null ? $"{h.Alumno.Nombre} {h.Alumno.Apellido} (individual)" : "Clase");
}
