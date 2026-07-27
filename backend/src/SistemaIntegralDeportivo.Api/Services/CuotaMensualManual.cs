using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Política de cuota ACTUAL: cada alumno paga un <b>valor mensual fijo</b> (su
/// <see cref="Alumno.Arancel"/>, cargado a mano por el profe). Genera UN cargo
/// <see cref="TipoCargo.Cuota"/> por alumno activo por mes.
///
/// Reglas de la costura (que la hacen segura de cambiar):
///  - <b>Solo alumnos con clase asignada:</b> el que no está en ningún grupo ni tiene
///    horario individual NO tiene cuota (aunque tenga arancel cargado en la ficha).
///  - <b>Idempotente:</b> si el alumno ya tiene su cargo Cuota del mes, no lo re-crea.
///  - <b>Sin arancel = no se cobra</b> (el profe lo verá como "cuota sin definir").
///  - <b>Solo el mes CORRIENTE:</b> no factura meses pasados (historia) ni adelanta
///    los futuros. Navegar por otros meses solo lee lo que ya existe.
/// </summary>
public class CuotaMensualManual : IPoliticaDeCuota
{
    private readonly IAlumnoRepository _alumnos;
    private readonly ICargoRepository _cargos;

    public CuotaMensualManual(IAlumnoRepository alumnos, ICargoRepository cargos)
    {
        _alumnos = alumnos;
        _cargos = cargos;
    }

    public async Task GenerarCargosDelMesAsync(int anio, int mes, CancellationToken ct = default)
    {
        // No facturamos meses pasados ni futuros: solo el corriente.
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        if (anio != hoy.Year || mes != hoy.Month) return;

        var activos = await _alumnos.ListarAsync(null, EstadoAlumno.Activo, ct);
        if (activos.Count == 0) return;

        // Solo se le cobra cuota al que toma clases (grupo activo u horario individual).
        var conClase = await _alumnos.ListarConClaseAsync(ct);

        // Idempotencia: qué alumnos ya tienen su cargo Cuota de este mes (los cargos
        // Cuota tienen TurnoId null, así que no los cubre el índice único por turno).
        var delMes = await _cargos.ListarDelMesAsync(anio, mes, ct);
        var yaConCuota = delMes
            .Where(c => c.Tipo == TipoCargo.Cuota)
            .Select(c => c.AlumnoId)
            .ToHashSet();

        var fecha = new DateOnly(anio, mes, 1);
        var concepto = $"Cuota {Meses[mes - 1]} {anio}";
        var genero = false;

        foreach (var alumno in activos)
        {
            if (!conClase.Contains(alumno.Id)) continue;                      // no toma clases → sin cuota
            if (alumno.Arancel is not { } arancel || arancel <= 0) continue;  // sin cuota definida
            if (yaConCuota.Contains(alumno.Id)) continue;                     // ya la tiene

            await _cargos.AgregarAsync(new Cargo
            {
                AlumnoId = alumno.Id,
                Tipo = TipoCargo.Cuota,
                Concepto = concepto,
                Monto = arancel,   // snapshot: cambiar el arancel después no toca el cargo emitido
                Fecha = fecha,
                // TenantId lo asigna el repositorio
            }, ct);
            genero = true;
        }

        if (genero) await _cargos.GuardarCambiosAsync(ct);
    }

    private static readonly string[] Meses =
    [
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre",
    ];
}
