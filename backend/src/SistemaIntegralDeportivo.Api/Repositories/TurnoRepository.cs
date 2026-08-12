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

    public async Task<IReadOnlyList<TurnoAgenda>> ListarEntreAsync(
        DateOnly desde, DateOnly hasta, CancellationToken ct = default) =>
        // Proyección y no Include: con dos colecciones en el mismo árbol (el roster de la
        // clase y los participantes del turno) EF hace UN join y las multiplica, y cada
        // fila arrastraba la ficha entera de dos alumnos —foto en base64 incluida—. Del
        // roster solo sale el título, y para eso alcanzan dos escalares: contarlo y, si
        // es uno solo, su nombre. Así queda UNA sola colección y se acaba el cartesiano.
        await _db.Turnos
            .AsNoTracking()
            .Where(t => t.TenantId == TenantId && t.Fecha >= desde && t.Fecha <= hasta)
            .Select(t => new TurnoAgenda(
                t.Id,
                t.Fecha,
                t.HoraInicio,
                t.DuracionMinutos,
                t.Estado,
                t.CanceladoMotivo,
                t.CanchaId,
                t.Cancha!.Nombre,
                t.Cancha.Sede!.Nombre,
                t.HorarioId,
                t.Horario!.Nombre,
                t.Horario.ProfesorUserId,
                t.Horario.ValorHoraProfe,
                t.Horario.Dia,
                t.Horario.HoraInicio,
                t.Horario.Alumnos.Count(x => x.FechaBaja == null),
                t.Horario.Alumnos
                    .Where(x => x.FechaBaja == null)
                    .Select(x => x.Alumno!.Nombre + " " + x.Alumno.Apellido)
                    .FirstOrDefault(),
                t.Participantes
                    .Select(p => new ParticipanteAgenda(
                        p.AlumnoId, p.Alumno!.Nombre, p.Alumno.Apellido, p.Presente))
                    .ToList()))
            .ToListAsync(ct);

    public async Task<ILookup<Guid, DateOnly>> FechasGeneradasAsync(
        IReadOnlyCollection<Guid> horarioIds, DateOnly desde, DateOnly hasta, CancellationToken ct = default)
    {
        if (horarioIds.Count == 0) return Enumerable.Empty<Guid>().ToLookup(x => x, _ => default(DateOnly));

        // Una sola consulta para todos los horarios del rango. Trae solo dos columnas,
        // así que aunque sean cientos de filas pesa nada: lo caro es el viaje, no los datos.
        var filas = await _db.Turnos
            .AsNoTracking()
            .Where(t => t.HorarioId != null && horarioIds.Contains(t.HorarioId.Value)
                        && t.Fecha >= desde && t.Fecha <= hasta)
            .Select(t => new { HorarioId = t.HorarioId!.Value, t.Fecha })
            .ToListAsync(ct);

        return filas.ToLookup(f => f.HorarioId, f => f.Fecha);
    }

    public Task<Turno?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Turnos
            .Include(t => t.Participantes)
            .FirstOrDefaultAsync(t => t.TenantId == TenantId && t.Id == id, ct);

    public async Task<IReadOnlyList<Turno>> ListarPorAlumnoEntreAsync(
        Guid alumnoId, DateOnly desde, DateOnly hasta, CancellationToken ct = default) =>
        await _db.Turnos
            .AsNoTracking()
            .Include(t => t.Cancha).ThenInclude(c => c.Sede)
            .Include(t => t.Horario).ThenInclude(h => h!.Alumnos).ThenInclude(ah => ah.Alumno)
            .Include(t => t.Participantes).ThenInclude(p => p.Alumno) // compañeros
            .Where(t => t.Fecha >= desde && t.Fecha <= hasta &&
                        t.Participantes.Any(p => p.AlumnoId == alumnoId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Turno>> ListarCanceladosRecientesAsync(
        int cantidad, CancellationToken ct = default) =>
        await _db.Turnos
            .AsNoTracking()
            .Include(t => t.Cancha)
            // El roster de la clase: de ahí sale su título (ver TurnoService.TituloDe).
            .Include(t => t.Horario).ThenInclude(h => h!.Alumnos).ThenInclude(ah => ah.Alumno)
            .Where(t => t.TenantId == TenantId && t.Estado == EstadoTurno.Cancelado)
            .OrderByDescending(t => t.CanceladoEl)
            .Take(cantidad)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TurnoParticipante>> ListarAvisosRecientesAsync(
        int cantidad, CancellationToken ct = default) =>
        await _db.TurnoParticipantes
            .AsNoTracking()
            .Include(p => p.Alumno)
            .Include(p => p.Turno).ThenInclude(t => t.Horario).ThenInclude(h => h!.Alumnos).ThenInclude(ah => ah.Alumno)
            .Where(p => p.Turno.TenantId == TenantId && p.CanceloEl != null)
            .OrderByDescending(p => p.CanceloEl)
            .Take(cantidad)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Turno>> ListarProgramadosDesdeAsync(
        DateOnly desde, Guid? canchaId, CancellationToken ct = default) =>
        await _db.Turnos
            .Include(t => t.Cancha)
            // El roster de la clase: de ahí sale su título (ver TurnoService.TituloDe).
            .Include(t => t.Horario).ThenInclude(h => h!.Alumnos).ThenInclude(ah => ah.Alumno)
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
