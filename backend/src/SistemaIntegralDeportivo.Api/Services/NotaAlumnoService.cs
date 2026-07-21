using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Notas del profe sobre un alumno (seguimiento). El profe elige compartir cada una.</summary>
public interface INotaAlumnoService
{
    /// <summary>soloCompartidas=true (portal del alumno): solo lo compartido. false: todas (profe).</summary>
    Task<IReadOnlyList<NotaAlumnoDto>> ListarAsync(Guid alumnoId, bool soloCompartidas, CancellationToken ct = default);
    Task<NotaAlumnoDto> CrearAsync(Guid alumnoId, CrearNotaAlumnoDto dto, CancellationToken ct = default);
    Task EliminarAsync(Guid id, CancellationToken ct = default);
}

public class NotaAlumnoService : INotaAlumnoService
{
    private readonly INotaAlumnoRepository _notas;
    private readonly IAlumnoRepository _alumnos;

    public NotaAlumnoService(INotaAlumnoRepository notas, IAlumnoRepository alumnos)
    {
        _notas = notas;
        _alumnos = alumnos;
    }

    public async Task<IReadOnlyList<NotaAlumnoDto>> ListarAsync(
        Guid alumnoId, bool soloCompartidas, CancellationToken ct = default)
    {
        var notas = await _notas.ListarPorAlumnoAsync(alumnoId, ct);
        // El portal del alumno NUNCA ve las privadas: el filtro vive acá, no en el repo.
        if (soloCompartidas) notas = notas.Where(n => n.Compartida).ToList();
        return notas.Select(Mapear).ToList();
    }

    public async Task<NotaAlumnoDto> CrearAsync(Guid alumnoId, CrearNotaAlumnoDto dto, CancellationToken ct = default)
    {
        // El repo de alumnos es tenant-scoped: si no lo encuentra, no es del club.
        _ = await _alumnos.ObtenerAsync(alumnoId, ct)
            ?? throw new ReglaDeNegocioException("El alumno no existe.");

        var nota = new NotaAlumno
        {
            AlumnoId = alumnoId,
            Texto = dto.Texto.Trim(),
            Compartida = dto.Compartida,
            // TenantId lo asigna el repositorio
        };
        await _notas.AgregarAsync(nota, ct);
        await _notas.GuardarCambiosAsync(ct);
        return Mapear(nota);
    }

    public async Task EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var nota = await _notas.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("La nota no existe.");

        _notas.Eliminar(nota);
        await _notas.GuardarCambiosAsync(ct);
    }

    private static NotaAlumnoDto Mapear(NotaAlumno n) => new()
    {
        Id = n.Id,
        Texto = n.Texto,
        Compartida = n.Compartida,
        CreadoEl = n.CreadoEl,
    };
}
