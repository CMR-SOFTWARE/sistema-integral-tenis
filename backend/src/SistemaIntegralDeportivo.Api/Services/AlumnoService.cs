using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class AlumnoService : IAlumnoService
{
    private readonly IAlumnoRepository _repo;
    private readonly ICargoRepository _cargos;
    private readonly ICredencialesService _credenciales;

    public AlumnoService(IAlumnoRepository repo, ICargoRepository cargos, ICredencialesService credenciales)
    {
        _repo = repo;
        _cargos = cargos;
        _credenciales = credenciales;
    }

    public async Task<AlumnoCreadoDto> CrearAsync(CreateAlumnoDto dto, CancellationToken ct = default)
    {
        // Orden anti-huérfanos: TODAS las reglas de la ficha se validan ANTES
        // de tocar Identity (si algo falla acá, no nace ningún usuario)
        if (await _repo.ExisteDniAsync(dto.Dni, ct))
            throw new ReglaDeNegocioException($"Ya existe un alumno con DNI {dto.Dni}.");
        ValidarMenor(dto);

        // Plan v2: el registro es UNA sola vez — el profe crea usuario + ficha
        // juntos; la temporal se muestra una vez y el alumno la cambia al entrar
        var cred = await _credenciales.CrearConTemporalAsync(
            dto.Email, dto.Nombre, dto.Apellido, dto.Dni, dto.Telefono, ct);

        try
        {
            var alumno = Construir(dto);
            alumno.UserId = cred.UserId;
            var creado = await _repo.AgregarAsync(alumno, ct);
            return new AlumnoCreadoDto
            {
                Alumno = Mapear(creado),
                Email = dto.Email.Trim(),
                PasswordTemporal = cred.PasswordTemporal,
            };
        }
        catch
        {
            // Compensación: la ficha falló (p.ej. carrera del índice único) →
            // el usuario recién creado no puede quedar huérfano
            await _credenciales.EliminarAsync(cred.UserId, ct);
            throw;
        }
    }

    public async Task<AccesoCreadoDto> CrearAccesoAsync(
        Guid alumnoId, string? email, CancellationToken ct = default)
    {
        var ficha = await _repo.ObtenerAsync(alumnoId, ct)
            ?? throw new ReglaDeNegocioException("El alumno no existe.");

        if (ficha.UserId is not null)
            throw new ReglaDeNegocioException("Este alumno ya tiene acceso al portal.");

        string mail;
        if (!string.IsNullOrWhiteSpace(email)) mail = email.Trim();
        else if (!string.IsNullOrWhiteSpace(ficha.Email)) mail = ficha.Email;
        else throw new ReglaDeNegocioException(
            "La ficha no tiene email: indicá uno para crear el acceso.");

        var cred = await _credenciales.CrearConTemporalAsync(
            mail, ficha.Nombre, ficha.Apellido, ficha.Dni, ficha.Telefono, ct);

        ficha.UserId = cred.UserId;
        ficha.Email = mail;
        ficha.ActualizadoEl = DateTime.UtcNow;
        await _repo.GuardarCambiosAsync(ct);

        return new AccesoCreadoDto { Email = mail, PasswordTemporal = cred.PasswordTemporal };
    }

    public async Task<AlumnoResponseDto> CrearVinculadoAsync(
        CreateAlumnoDto dto, Guid userId, CancellationToken ct = default)
    {
        // Aprobación de solicitud: el usuario YA existe — nada de credenciales.
        // Si el profe ya lo tenía cargado (mismo DNI, ficha libre), se vincula
        // ESA ficha en vez de duplicar: el reemplazo elegante del reclamo.
        var existente = await _repo.ObtenerPorDniAsync(dto.Dni, ct);
        if (existente is not null)
        {
            // Ya vinculada a ESTE usuario: idempotente (re-aprobar no rompe)
            if (existente.UserId == userId) return Mapear(existente);

            if (existente.UserId is not null)
                throw new ReglaDeNegocioException(
                    $"Ya existe un alumno con DNI {dto.Dni} vinculado a otra cuenta.");

            existente.UserId = userId;
            existente.ActualizadoEl = DateTime.UtcNow;
            await _repo.GuardarCambiosAsync(ct);
            return Mapear(existente);
        }

        ValidarMenor(dto);

        var alumno = Construir(dto);
        alumno.UserId = userId;
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

    /// <summary>Regla del menor (Ley 25.326): tutor + consentimiento obligatorios.</summary>
    private static void ValidarMenor(CreateAlumnoDto dto)
    {
        if (CalcularEdad(dto.FechaNacimiento) >= 18) return;

        if (dto.Tutor is null)
            throw new ReglaDeNegocioException("Un alumno menor de edad requiere un tutor.");
        if (!dto.ConsentimientoDatos)
            throw new ReglaDeNegocioException(
                "Un alumno menor requiere el consentimiento de datos otorgado por su tutor.");
    }

    /// <summary>La entidad desde el DTO (consentimientos con timestamp del server).</summary>
    private static Alumno Construir(CreateAlumnoDto dto)
    {
        var ahora = DateTime.UtcNow;
        return new Alumno
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
        TieneUsuario = a.UserId is not null,
    };
}
