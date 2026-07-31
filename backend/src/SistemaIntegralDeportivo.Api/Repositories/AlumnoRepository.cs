using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Repositories;

public class AlumnoRepository : IAlumnoRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantActual _tenantActual;

    // El tenant sale del token o del override del portal (ADR-0010)
    private Guid TenantId => _tenantActual.TenantId;

    public AlumnoRepository(AppDbContext db, ITenantActual tenantActual)
    {
        _db = db;
        _tenantActual = tenantActual;
    }

    public Task<bool> ExisteDniAsync(string dni, CancellationToken ct = default) =>
        _db.Alumnos.AnyAsync(a => a.TenantId == TenantId && a.Dni == dni, ct);

    public Task<bool> ExisteFichaConNombreYTelefonoAsync(
        string nombre, string apellido, string telefono, CancellationToken ct = default)
    {
        var n = nombre.Trim().ToLower();
        var a = apellido.Trim().ToLower();
        return _db.Alumnos.AnyAsync(x =>
            x.TenantId == TenantId
            && x.Telefono == telefono
            && x.Nombre.ToLower() == n
            && x.Apellido.ToLower() == a, ct);
    }

    public Task<bool> EsAlumnoDelTenantAsync(Guid userId, CancellationToken ct = default) =>
        _db.Alumnos.AnyAsync(a => a.TenantId == TenantId && a.UserId == userId, ct);

    public Task<Alumno?> ObtenerPorDniAsync(string dni, CancellationToken ct = default) =>
        _db.Alumnos.FirstOrDefaultAsync(a => a.TenantId == TenantId && a.Dni == dni, ct);

    public async Task<Alumno> AgregarAsync(Alumno alumno, CancellationToken ct = default)
    {
        alumno.TenantId = TenantId;

        if (alumno.Tutor is not null)
        {
            // Si el tutor ya existe en el tenant (mismo DNI: caso hermanos),
            // se reutiliza en vez de duplicarlo (índice único TenantId+Dni).
            var existente = await _db.Tutores.FirstOrDefaultAsync(
                t => t.TenantId == TenantId && t.Dni == alumno.Tutor.Dni, ct);

            if (existente is not null)
            {
                alumno.Tutor = null;
                alumno.TutorId = existente.Id;
            }
            else
            {
                alumno.Tutor.TenantId = TenantId;
            }
        }

        _db.Alumnos.Add(alumno);
        await _db.SaveChangesAsync(ct);
        return alumno;
    }

    public async Task VincularTutorAsync(Alumno alumno, Tutor tutor, CancellationToken ct = default)
    {
        // Mismo criterio que el alta: si ya hay un tutor con ese DNI en el tenant se
        // REUTILIZA (índice único TenantId+Dni); si no, se crea el nuevo. NO guarda:
        // el caller persiste con GuardarCambiosAsync (misma transacción que la ficha).
        var existente = await _db.Tutores.FirstOrDefaultAsync(
            t => t.TenantId == TenantId && t.Dni == tutor.Dni, ct);
        if (existente is not null)
        {
            alumno.Tutor = null;
            alumno.TutorId = existente.Id;
        }
        else
        {
            // El alumno YA está trackeado: colgar el tutor por navegación haría que EF
            // adivine el estado por la PK. Como Tutor.Id ya viene con Guid.NewGuid() (no
            // vacío), EF lo tomaría como Modified → UPDATE en vez de INSERT → el tutor
            // nunca se inserta y la FK del alumno apunta a un id inexistente (23503).
            // Add() fuerza el estado Added sin importar el valor de la clave (como el alta).
            tutor.TenantId = TenantId;
            _db.Tutores.Add(tutor);
            alumno.Tutor = tutor;
        }
    }

    public async Task<IReadOnlyList<Alumno>> ListarAsync(
        CategoriaAlumno? categoria, EstadoAlumno? estado, CancellationToken ct = default)
    {
        var query = _db.Alumnos.AsNoTracking().Include(a => a.Sede).Where(a => a.TenantId == TenantId);

        if (categoria is not null) query = query.Where(a => a.Categoria == categoria);
        if (estado is not null) query = query.Where(a => a.Estado == estado);
        // Sin filtro explícito, la lista principal NO muestra a los de la lista de
        // espera (todavía no son alumnos). Se ven pidiendo estado=EnEspera.
        else query = query.Where(a => a.Estado != EstadoAlumno.EnEspera);

        return await query
            .OrderBy(a => a.Apellido).ThenBy(a => a.Nombre)
            .ToListAsync(ct);
    }

    public async Task PromoverDeEsperaAsync(Guid alumnoId, CancellationToken ct = default)
    {
        var alumno = await _db.Alumnos.FirstOrDefaultAsync(
            a => a.TenantId == TenantId && a.Id == alumnoId && a.Estado == EstadoAlumno.EnEspera, ct);
        if (alumno is not null)
        {
            alumno.Estado = EstadoAlumno.Activo;
            alumno.ActualizadoEl = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    public Task<Alumno?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        _db.Alumnos.Include(a => a.Sede).FirstOrDefaultAsync(a => a.TenantId == TenantId && a.Id == id, ct);

    public async Task<HashSet<Guid>> ListarConClaseAsync(CancellationToken ct = default)
    {
        // En un grupo activo (membresía sin baja) del tenant
        var enGrupos = _db.AlumnoGrupos
            .Where(m => m.FechaBaja == null && m.Grupo.TenantId == TenantId)
            .Select(m => m.AlumnoId);

        // Con un horario individual activo del tenant
        var conHorario = _db.Horarios
            .Where(h => h.TenantId == TenantId && h.Activo && h.AlumnoId != null)
            .Select(h => h.AlumnoId!.Value);

        var ids = await enGrupos.Concat(conHorario).Distinct().ToListAsync(ct);
        return ids.ToHashSet();
    }

    public async Task EliminarDefinitivoAsync(Alumno alumno, CancellationToken ct = default)
    {
        // Los horarios individuales del alumno (y sus turnos) NO caen en cascada:
        // la FK Horario→Alumno es SetNull. Los borramos a mano para no dejar
        // horarios huérfanos. Los TurnoParticipante de esos turnos sí caen solos.
        var horarios = await _db.Horarios
            .Include(h => h.Turnos)
            .Where(h => h.TenantId == TenantId && h.AlumnoId == alumno.Id)
            .ToListAsync(ct);

        foreach (var h in horarios)
            _db.Turnos.RemoveRange(h.Turnos);
        _db.Horarios.RemoveRange(horarios);

        // La ficha: cargos, participaciones, membresías, raquetas, notas y
        // solicitudes (grupo/horario/clase suelta) caen en cascada.
        _db.Alumnos.Remove(alumno);
        await _db.SaveChangesAsync(ct);
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);

    public Task RecargarSedeAsync(Alumno alumno, CancellationToken ct = default) =>
        _db.Entry(alumno).Reference(a => a.Sede).LoadAsync(ct);

    // ── Auth / portal: GLOBAL a propósito (no scopea por tenant): la
    //    identidad cruza negocios (ADR-0007) ──

    public Task<Alumno?> ObtenerPorUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.Alumnos
            .Include(a => a.Tenant)
            .OrderBy(a => a.Nombre)
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

    public async Task<IReadOnlyList<Alumno>> ListarPorUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _db.Alumnos
            .Include(a => a.Tenant)
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Nombre)
            .ToListAsync(ct);

    public Task<int> ContarPorEstadoAsync(EstadoAlumno estado, CancellationToken ct = default) =>
        _db.Alumnos.CountAsync(a => a.TenantId == TenantId && a.Estado == estado, ct);

    public Task<int> ContarNuevosDesdeAsync(DateTime desde, CancellationToken ct = default) =>
        _db.Alumnos.CountAsync(a => a.TenantId == TenantId && a.CreadoEl >= desde, ct);

    public async Task<decimal> SumarArancelActivosAsync(CancellationToken ct = default) =>
        await _db.Alumnos
            .Where(a => a.TenantId == TenantId && a.Estado == EstadoAlumno.Activo)
            .SumAsync(a => a.Arancel ?? 0, ct);

    public async Task<Dictionary<CategoriaAlumno, int>> ContarPorCategoriaAsync(CancellationToken ct = default) =>
        await _db.Alumnos
            .Where(a => a.TenantId == TenantId
                && a.Estado != EstadoAlumno.Inactivo && a.Estado != EstadoAlumno.EnEspera)
            .GroupBy(a => a.Categoria)
            .Select(g => new { Categoria = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.Categoria, x => x.Cantidad, ct);
}
