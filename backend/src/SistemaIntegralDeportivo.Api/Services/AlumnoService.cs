using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class AlumnoService : IAlumnoService
{
    private readonly IAlumnoRepository _repo;
    private readonly ICargoRepository _cargos;

    public AlumnoService(IAlumnoRepository repo, ICargoRepository cargos)
    {
        _repo = repo;
        _cargos = cargos;
    }

    public async Task<AlumnoResponseDto> CrearAsync(CreateAlumnoDto dto, CancellationToken ct = default)
    {
        // Regla: DNI único por tenant
        if (await _repo.ExisteDniAsync(dto.Dni, ct))
            throw new ReglaDeNegocioException($"Ya existe un alumno con DNI {dto.Dni}.");

        // Regla del menor (Ley 25.326): tutor + consentimiento obligatorios
        var esMenor = CalcularEdad(dto.FechaNacimiento) < 18;
        if (esMenor)
        {
            if (dto.Tutor is null)
                throw new ReglaDeNegocioException("Un alumno menor de edad requiere un tutor.");
            if (!dto.ConsentimientoDatos)
                throw new ReglaDeNegocioException(
                    "Un alumno menor requiere el consentimiento de datos otorgado por su tutor.");
        }

        // Consentimientos: el bool viene del DTO; el timestamp lo pone el server
        var ahora = DateTime.UtcNow;
        var alumno = new Alumno
        {
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Dni = dto.Dni,
            Telefono = dto.Telefono,
            Email = dto.Email,
            FechaNacimiento = dto.FechaNacimiento,
            Categoria = dto.Categoria,
            Arancel = dto.Arancel,
            Notas = dto.Notas,
            ConsentimientoWhatsapp = dto.ConsentimientoWhatsapp,
            ConsentimientoWhatsappEl = dto.ConsentimientoWhatsapp ? ahora : null,
            ConsentimientoDatos = dto.ConsentimientoDatos,
            ConsentimientoDatosEl = dto.ConsentimientoDatos ? ahora : null,
            Tutor = dto.Tutor is null
                ? null
                : new Tutor
                {
                    Nombre = dto.Tutor.Nombre,
                    Apellido = dto.Tutor.Apellido,
                    Dni = dto.Tutor.Dni,
                    Telefono = dto.Tutor.Telefono,
                    Relacion = dto.Tutor.Relacion,
                },
            // TenantId lo asigna el repositorio (dueño del scoping por tenant)
        };

        var creado = await _repo.AgregarAsync(alumno, ct);
        return Mapear(creado);
    }

    public async Task<IReadOnlyList<AlumnoResponseDto>> ListarAsync(
        CategoriaAlumno? categoria, EstadoAlumno? estado, CancellationToken ct = default)
    {
        var alumnos = await _repo.ListarAsync(categoria, estado, ct);
        var deudores = await DeudoresDeAsync(alumnos.Select(a => a.Id).ToList(), ct);
        return alumnos.Select(a => Mapear(a, deudores.Contains(a.Id))).ToList();
    }

    public async Task<AlumnoResponseDto?> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        var alumno = await _repo.ObtenerAsync(id, ct);
        if (alumno is null) return null;

        var deudores = await DeudoresDeAsync([id], ct);
        return Mapear(alumno, deudores.Contains(id));
    }

    public async Task<AlumnoResponseDto?> CambiarEstadoAsync(
        Guid id, EstadoAlumno estado, CancellationToken ct = default)
    {
        var alumno = await _repo.ObtenerAsync(id, ct);
        if (alumno is null) return null;

        alumno.Estado = estado;
        alumno.ActualizadoEl = DateTime.UtcNow;
        await _repo.GuardarCambiosAsync(ct);
        return Mapear(alumno);
    }

    public async Task<bool> DarDeBajaAsync(Guid id, CancellationToken ct = default)
    {
        // Baja LÓGICA (regla no negociable: estados, no borrados)
        var alumno = await _repo.ObtenerAsync(id, ct);
        if (alumno is null) return false;

        alumno.Estado = EstadoAlumno.Inactivo;
        alumno.ActualizadoEl = DateTime.UtcNow;
        await _repo.GuardarCambiosAsync(ct);
        return true;
    }

    /// <summary>Alumnos con cuota vencida (señal en la ficha; la regla vive en CuotaService).</summary>
    private async Task<HashSet<Guid>> DeudoresDeAsync(IReadOnlyCollection<Guid> alumnoIds, CancellationToken ct)
    {
        if (alumnoIds.Count == 0) return [];

        var impagos = await _cargos.ListarImpagosAsync(alumnoIds, ct);
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        return impagos
            .GroupBy(c => c.AlumnoId)
            .Where(g => CuotaService.TieneDeudaVencida(g, hoy))
            .Select(g => g.Key)
            .ToHashSet();
    }

    /// <summary>Edad en años cumplidos a hoy (esMenor nunca se guarda: se calcula).</summary>
    private static int CalcularEdad(DateTime nacimiento)
    {
        var hoy = DateTime.UtcNow.Date;
        var edad = hoy.Year - nacimiento.Year;
        if (nacimiento.Date > hoy.AddYears(-edad)) edad--;
        return edad;
    }

    private static AlumnoResponseDto Mapear(Alumno a, bool deudaVencida = false) => new()
    {
        Id = a.Id,
        Nombre = a.Nombre,
        Apellido = a.Apellido,
        Dni = a.Dni,
        Telefono = a.Telefono,
        Email = a.Email,
        FechaNacimiento = a.FechaNacimiento,
        EsMenor = CalcularEdad(a.FechaNacimiento) < 18,
        Categoria = a.Categoria.ToString(),
        Estado = a.Estado.ToString(),
        Arancel = a.Arancel,
        Notas = a.Notas,
        TutorId = a.TutorId ?? a.Tutor?.Id,
        CreadoEl = a.CreadoEl,
        DeudaVencida = deudaVencida,
    };
}
