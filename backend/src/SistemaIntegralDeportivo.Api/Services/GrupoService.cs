using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class GrupoService : IGrupoService
{
    private readonly IGrupoRepository _grupos;
    private readonly IAlumnoRepository _alumnos;
    private readonly ICargoRepository _cargos;
    private readonly IAlumnoService _alumnoService;

    public GrupoService(
        IGrupoRepository grupos, IAlumnoRepository alumnos, ICargoRepository cargos,
        IAlumnoService alumnoService)
    {
        _grupos = grupos;
        _alumnos = alumnos;
        _cargos = cargos;
        _alumnoService = alumnoService;
    }

    public async Task<GrupoResponseDto> CrearAsync(CreateGrupoDto dto, CancellationToken ct = default)
    {
        var grupo = new Grupo
        {
            Nombre = dto.Nombre,
            Categoria = dto.Categoria,
            CupoMaximo = dto.CupoMaximo,
            // TenantId lo asigna el repositorio (dueño del scoping)
        };

        var creado = await _grupos.AgregarAsync(grupo, ct);
        return Mapear(creado);
    }

    public async Task<IReadOnlyList<GrupoResponseDto>> ListarAsync(CancellationToken ct = default)
    {
        var grupos = await _grupos.ListarAsync(ct);
        return grupos.Select(Mapear).ToList();
    }

    public async Task<GrupoResponseDto?> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        var grupo = await _grupos.ObtenerAsync(id, ct);
        return grupo is null ? null : Mapear(grupo);
    }

    public async Task AsignarAlumnoAsync(Guid grupoId, Guid alumnoId, CancellationToken ct = default)
    {
        var grupo = await _grupos.ObtenerAsync(grupoId, ct)
            ?? throw new ReglaDeNegocioException("El grupo no existe.");

        var alumno = await _alumnos.ObtenerAsync(alumnoId, ct)
            ?? throw new ReglaDeNegocioException("El alumno no existe.");

        // Regla: solo alumnos ACTIVOS se asignan (un suspendido no reserva)
        if (alumno.Estado != EstadoAlumno.Activo)
            throw new ReglaDeNegocioException(
                $"{alumno.Nombre} {alumno.Apellido} no está activo y no puede asignarse a un grupo.");

        // Regla: nadie toma clases NUEVAS con la cuota vencida (las ya asignadas siguen)
        var impagos = await _cargos.ListarImpagosAsync([alumnoId], ct);
        if (CuotaService.TieneDeudaVencida(impagos, DateOnly.FromDateTime(DateTime.UtcNow)))
            throw new ReglaDeNegocioException(
                $"{alumno.Nombre} {alumno.Apellido} tiene la cuota vencida: registrá el pago en Cuotas antes de sumarlo a un grupo.");

        // Regla: no duplicar miembro activo; si tuvo baja previa, se reactiva
        var membresia = await _grupos.ObtenerMembresiaAsync(grupoId, alumnoId, ct);
        if (membresia is not null && membresia.FechaBaja is null)
            throw new ReglaDeNegocioException(
                $"{alumno.Nombre} {alumno.Apellido} ya es miembro del grupo.");

        // Regla: respetar el cupo (null = sin límite)
        if (grupo.CupoMaximo is not null)
        {
            var activos = await _grupos.ContarMiembrosActivosAsync(grupoId, ct);
            if (activos >= grupo.CupoMaximo)
                throw new ReglaDeNegocioException(
                    $"El grupo \"{grupo.Nombre}\" está completo ({grupo.CupoMaximo} lugares).");
        }

        if (membresia is not null)
        {
            // Reactivación: la PK compuesta (AlumnoId, GrupoId) no admite otra
            // fila, así que se reutiliza la existente. Limitación consciente:
            // se pierde el detalle del período anterior.
            membresia.FechaAlta = DateTime.UtcNow;
            membresia.FechaBaja = null;
        }
        else
        {
            await _grupos.AgregarMembresiaAsync(new AlumnoGrupo
            {
                GrupoId = grupoId,
                AlumnoId = alumnoId,
            }, ct);
        }

        // Persistir la membresía ANTES de reconciliar: la reconciliación
        // consulta las membresías activas del alumno (query a la base) y tiene
        // que ver ya la que acabamos de dar de alta.
        await _grupos.GuardarCambiosAsync(ct);

        // Sumarlo a un grupo lo repone en los turnos futuros YA generados de
        // ese grupo y recalcula el divisor: sin esto, el que vuelve no aparece
        // en el calendario ni se le genera cuota (el bug de Lucas al re-agregar
        // a un alumno tras darlo de baja).
        await _alumnoService.SincronizarCalendarioAsync(alumnoId, ct);
        await _grupos.GuardarCambiosAsync(ct);
    }

    public async Task QuitarAlumnoAsync(Guid grupoId, Guid alumnoId, CancellationToken ct = default)
    {
        var membresia = await _grupos.ObtenerMembresiaAsync(grupoId, alumnoId, ct);
        if (membresia is null || membresia.FechaBaja is not null)
            throw new ReglaDeNegocioException("El alumno no es miembro activo del grupo.");

        // Baja lógica: se conserva la historia ("estuvo de marzo a junio")
        membresia.FechaBaja = DateTime.UtcNow;
        await _grupos.GuardarCambiosAsync(ct);

        // Ya no es de este grupo: sacarlo de sus turnos futuros (sigue Activo y
        // en sus OTROS grupos, así que la reconciliación solo lo saca de este).
        await _alumnoService.SincronizarCalendarioAsync(alumnoId, ct);
        await _grupos.GuardarCambiosAsync(ct);
    }

    private static GrupoResponseDto Mapear(Grupo g)
    {
        var activos = g.Alumnos.Where(m => m.FechaBaja is null).ToList();
        return new GrupoResponseDto
        {
            Id = g.Id,
            Nombre = g.Nombre,
            Categoria = g.Categoria?.ToString(),
            CupoMaximo = g.CupoMaximo,
            Activo = g.Activo,
            MiembrosActivos = activos.Count,
            Miembros = activos
                .Where(m => m.Alumno is not null)
                .Select(m => new MiembroGrupoDto
                {
                    AlumnoId = m.AlumnoId,
                    Nombre = m.Alumno.Nombre,
                    Apellido = m.Alumno.Apellido,
                    Categoria = m.Alumno.Categoria.ToString(),
                    FechaAlta = m.FechaAlta,
                })
                .ToList(),
        };
    }
}
