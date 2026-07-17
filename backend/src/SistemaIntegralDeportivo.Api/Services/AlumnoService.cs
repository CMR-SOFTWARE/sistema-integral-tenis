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
    private readonly ITurnoRepository _turnos;
    private readonly IGrupoRepository _grupos;
    private readonly IHorarioRepository _horarios;

    public AlumnoService(
        IAlumnoRepository repo, ICargoRepository cargos, ICredencialesService credenciales,
        ITurnoRepository turnos, IGrupoRepository grupos, IHorarioRepository horarios)
    {
        _repo = repo;
        _cargos = cargos;
        _credenciales = credenciales;
        _turnos = turnos;
        _grupos = grupos;
        _horarios = horarios;
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

    public async Task<AlumnoResponseDto> EditarAsync(
        Guid id, UpdateAlumnoDto dto, CancellationToken ct = default)
    {
        var alumno = await _repo.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("El alumno no existe.");

        // DNI único por tenant, pero sin chocar contra uno mismo
        if (dto.Dni != alumno.Dni)
        {
            var dueño = await _repo.ObtenerPorDniAsync(dto.Dni, ct);
            if (dueño is not null && dueño.Id != alumno.Id)
                throw new ReglaDeNegocioException($"Ya existe un alumno con DNI {dto.Dni}.");
        }

        // Corregir la fecha no puede dejar un menor sin tutor (Ley 25.326)
        if (CalcularEdad(dto.FechaNacimiento) < 18 && alumno.TutorId is null && alumno.Tutor is null)
            throw new ReglaDeNegocioException(
                "Con esa fecha el alumno es menor: necesita un tutor cargado.");

        alumno.Nombre = dto.Nombre;
        alumno.Apellido = dto.Apellido;
        alumno.Dni = dto.Dni;
        alumno.Telefono = dto.Telefono;
        alumno.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        alumno.FechaNacimiento = dto.FechaNacimiento;
        alumno.Categoria = dto.Categoria;
        alumno.Modalidad = dto.Modalidad;
        alumno.Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas;
        alumno.ActualizadoEl = DateTime.UtcNow;

        await _repo.GuardarCambiosAsync(ct);
        return Mapear(alumno);
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

        // El estado manda sobre el calendario: el que no está activo no ocupa
        // turnos futuros ni paga clases a las que no va; el que vuelve a Activo
        // regresa solo a los turnos de sus grupos. Una sola reconciliación.
        await SincronizarCalendarioAsync(id, ct);

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

        // A diferencia de la pausa (que le GUARDA el lugar), la baja lo LIBERA:
        // se va de sus grupos (cupo) y suelta su cancha (horario). Se libera
        // ANTES de reconciliar para que la reconciliación ya no lo vea en
        // ningún grupo activo y lo saque de todos sus turnos futuros.
        await LiberarLugarAsync(id, ct);
        await SincronizarCalendarioAsync(id, ct);

        await _repo.GuardarCambiosAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<MorosoDto>> ListarMorososAsync(CancellationToken ct = default)
    {
        var activos = await _repo.ListarAsync(null, EstadoAlumno.Activo, ct);
        if (activos.Count == 0) return [];

        var impagos = await _cargos.ListarImpagosAsync(activos.Select(a => a.Id).ToList(), ct);
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        return impagos
            .GroupBy(c => c.AlumnoId)
            .Where(g => CuotaService.DebeSuspenderse(g, hoy))
            .Select(g =>
            {
                var alumno = activos.First(a => a.Id == g.Key);
                return new MorosoDto
                {
                    Id = alumno.Id,
                    Nombre = alumno.Nombre,
                    Apellido = alumno.Apellido,
                    Telefono = alumno.Telefono,
                    Deuda = g.Sum(c => c.Monto),
                    MesesAdeudados = string.Join(", ", g
                        .Select(c => new DateOnly(c.Fecha.Year, c.Fecha.Month, 1))
                        .Distinct()
                        .OrderBy(f => f)
                        .Select(f => MesEnEspanol(f))),
                };
            })
            .OrderByDescending(m => m.Deuda)
            .ToList();
    }

    private static readonly string[] Meses =
    [
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre",
    ];

    /// <summary>"Junio" o "Junio 2025" si es de otro año (para que no confunda).</summary>
    private static string MesEnEspanol(DateOnly fecha) =>
        fecha.Year == DateTime.UtcNow.Year
            ? Meses[fecha.Month - 1]
            : $"{Meses[fecha.Month - 1]} {fecha.Year}";

    /// <summary>
    /// Reconcilia los turnos futuros del alumno con su realidad actual. Es la
    /// ÚNICA fuente de verdad del vínculo estado↔calendario: la llaman el
    /// cambio de estado, la baja y también GrupoService al entrar/salir de un
    /// grupo (antes cada camino la copiaba —o se la olvidaba, que fue el bug—).
    ///
    ///  - Donde DEBE estar y no está (grupos activos + horarios individuales,
    ///    solo si Activo) → lo agrega al roster de los turnos futuros ya
    ///    generados. Los individuales borrados los regenera la generación
    ///    perezosa; acá solo se completan los que existan.
    ///  - Donde ESTÁ y no debe (pausado, o ya no es de ese grupo) → lo saca; si
    ///    era el único (individual), el turno entero se va y libera el slot.
    ///  - Cada turno TOCADO invalida sus cargos IMPAGOS: el divisor cambió
    ///    (÷3 ↔ ÷4) y la liquidación los regenera con el correcto (la fórmula
    ///    vive solo en CuotaService). Lo PAGADO es intocable.
    ///
    /// NO persiste: el caller hace un único GuardarCambios (mismo DbContext).
    /// </summary>
    public async Task SincronizarCalendarioAsync(Guid alumnoId, CancellationToken ct = default)
    {
        var alumno = await _repo.ObtenerAsync(alumnoId, ct);
        if (alumno is null) return;

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var tocados = new HashSet<Guid>();

        // Turnos futuros donde DEBERÍA estar (solo si está Activo)
        var debeEstar = new Dictionary<Guid, Turno>();
        if (alumno.Estado == EstadoAlumno.Activo)
        {
            var misGrupos = (await _grupos.ListarMembresiasActivasDeAlumnoAsync(alumnoId, ct))
                .Select(m => m.GrupoId)
                .ToHashSet();
            var horarios = (await _horarios.ListarActivosAsync(ct))
                .Where(h => (h.GrupoId is not null && misGrupos.Contains(h.GrupoId.Value))
                         || h.AlumnoId == alumnoId);

            foreach (var horario in horarios)
                foreach (var turno in await _turnos.ListarPorHorarioDesdeAsync(horario.Id, hoy, ct))
                    if (turno.Estado == EstadoTurno.Programado)
                        debeEstar[turno.Id] = turno;
        }

        // Turnos futuros donde participa HOY
        var participaEn = await _turnos.ListarFuturosDeAlumnoAsync(alumnoId, hoy, ct);

        // 1) Reponer donde debe estar y todavía no está
        foreach (var (turnoId, turno) in debeEstar)
        {
            if (turno.Participantes.Any(p => p.AlumnoId == alumnoId)) continue;
            turno.Participantes.Add(new TurnoParticipante { TurnoId = turnoId, AlumnoId = alumnoId });
            tocados.Add(turnoId);
        }

        // 2) Sacar donde está pero ya no debe (respetando lo PAGADO)
        if (participaEn.Count > 0)
        {
            var cargos = await _cargos.ListarPorTurnosAsync(participaEn.Select(t => t.Id).ToList(), ct);
            var porTurno = cargos.ToLookup(c => c.TurnoId!.Value);

            foreach (var turno in participaEn)
            {
                if (debeEstar.ContainsKey(turno.Id)) continue; // debe seguir: no se toca

                var miCargo = porTurno[turno.Id].FirstOrDefault(c => c.AlumnoId == alumnoId);
                if (miCargo?.PagadoEl is not null) continue; // ya lo cobró: intocable

                var mia = turno.Participantes.FirstOrDefault(p => p.AlumnoId == alumnoId);
                if (mia is null) continue;

                if (turno.Participantes.Count == 1) _turnos.Eliminar(turno);
                else _turnos.QuitarParticipante(mia);
                tocados.Add(turno.Id);
            }
        }

        // 3) El roster de los turnos tocados cambió → invalidar sus cargos
        // IMPAGOS (el mío y los de los compañeros) para que la liquidación los
        // recalcule con el nuevo divisor. Los pagados quedan.
        if (tocados.Count > 0)
        {
            var cargos = await _cargos.ListarPorTurnosAsync(tocados.ToList(), ct);
            foreach (var cargo in cargos.Where(c => c.PagadoEl is null))
                _cargos.Eliminar(cargo);
        }
    }

    /// <summary>Baja: libera el cupo de sus grupos y el slot de sus horarios individuales.</summary>
    private async Task LiberarLugarAsync(Guid alumnoId, CancellationToken ct)
    {
        var ahora = DateTime.UtcNow;

        foreach (var membresia in await _grupos.ListarMembresiasActivasDeAlumnoAsync(alumnoId, ct))
            membresia.FechaBaja = ahora;

        foreach (var horario in await _horarios.ListarIndividualesDeAlumnoAsync(alumnoId, ct))
            horario.Activo = false;
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
        Modalidad = a.Modalidad.ToString(),
        Arancel = a.Arancel,
        Notas = a.Notas,
        TutorId = a.TutorId ?? a.Tutor?.Id,
        CreadoEl = a.CreadoEl,
        DeudaVencida = deudaVencida,
        TieneUsuario = a.UserId is not null,
        FotoUrl = a.FotoUrl,
    };
}
