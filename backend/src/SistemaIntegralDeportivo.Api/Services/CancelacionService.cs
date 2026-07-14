using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class CancelacionService : ICancelacionService
{
    private readonly ITurnoRepository _turnos;

    public CancelacionService(ITurnoRepository turnos)
    {
        _turnos = turnos;
    }

    public async Task<IReadOnlyList<CancelacionDto>> ListarRecientesAsync(
        int cantidad, CancellationToken ct = default)
    {
        // Dos fuentes: turnos enteros cancelados + avisos individuales.
        // Se piden 'cantidad' de cada una y el merge se queda con las
        // 'cantidad' más recientes del total.
        var cancelados = await _turnos.ListarCanceladosRecientesAsync(cantidad, ct);
        var avisos = await _turnos.ListarAvisosRecientesAsync(cantidad, ct);

        return cancelados.Select(Mapear)
            .Concat(avisos.Select(Mapear))
            .OrderByDescending(c => c.CanceladoEl)
            .Take(cantidad)
            .ToList();
    }

    /// <summary>Turno ENTERO cancelado (por el profe, a mano o vía bloqueo).</summary>
    private static CancelacionDto Mapear(Turno t) => new()
    {
        Fecha = t.Fecha,
        HoraInicio = t.HoraInicio,
        Titulo = Titulo(t),
        Motivo = t.CanceladoMotivo,
        Por = (t.CanceladoPor ?? CanceladoPor.Profesor).ToString(), // legacy sin dato = profe
        // En clase individual hay UN afectado concreto a quien avisar
        AlumnoNombre = t.Horario?.Alumno is { } a ? $"{a.Nombre} {a.Apellido}" : null,
        Telefono = t.Horario?.Alumno?.Telefono,
        CanceladoEl = t.CanceladoEl ?? default,
    };

    /// <summary>Aviso individual de un alumno (el turno siguió en pie).</summary>
    private static CancelacionDto Mapear(TurnoParticipante p) => new()
    {
        Fecha = p.Turno.Fecha,
        HoraInicio = p.Turno.HoraInicio,
        Titulo = Titulo(p.Turno),
        Motivo = p.CancelacionMotivo,
        Por = CanceladoPor.Alumno.ToString(),
        AlumnoNombre = p.Alumno is not null ? $"{p.Alumno.Nombre} {p.Alumno.Apellido}" : null,
        Telefono = p.Alumno?.Telefono,
        CanceladoEl = p.CanceloEl ?? default,
    };

    // Mismo criterio que TurnoService.Mapear
    private static string Titulo(Turno t) =>
        t.Horario?.Grupo?.Nombre
            ?? (t.Horario?.Alumno is not null
                ? $"{t.Horario.Alumno.Nombre} {t.Horario.Alumno.Apellido} (individual)"
                : string.Empty);
}
