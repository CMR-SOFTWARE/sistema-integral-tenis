using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>Profes empleados (Staff) del club: los administra el DUEÑO del tenant.</summary>
public interface IStaffService
{
    Task<IReadOnlyList<StaffDto>> ListarAsync(CancellationToken ct = default);
    /// <summary>El dueño le crea la cuenta al profe (como a un alumno) y lo suma como Staff.</summary>
    Task<StaffCreadoDto> AgregarAsync(AgregarStaffDto dto, CancellationToken ct = default);
    /// <summary>Baja/reactivación del profe empleado.</summary>
    Task CambiarActivoAsync(Guid id, bool activo, CancellationToken ct = default);

    /// <summary>Setea/actualiza el valor hora base del profe (null = borrarlo).</summary>
    Task CambiarValorHoraAsync(Guid id, decimal? valorHora, CancellationToken ct = default);

    /// <summary>Edita la ficha del empleado (datos personales + valor hora; el celular/login no).</summary>
    Task EditarAsync(Guid id, UpdateStaffDto dto, CancellationToken ct = default);

    /// <summary>
    /// Edita los datos del DIRECTOR (el dueño). Va aparte porque no tiene membresía:
    /// solo se tocan sus datos personales, sin valor hora ni club.
    /// </summary>
    Task EditarDirectorAsync(UpdateStaffDto dto, CancellationToken ct = default);

    /// <summary>
    /// Borrado REAL del profe empleado (el dueño se equivocó al cargarlo y lo quiere
    /// eliminar, no solo desactivar). Saca la membresía, deja "sin asignar" lo que lo
    /// tenía de profe (grupos/horarios/alumnos), y borra su login si no cumple otro rol.
    /// </summary>
    Task EliminarDefinitivoAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Los profes a los que el dueño puede asignar clases: el dueño + los staff ACTIVOS.
    /// Con <paramref name="sedeId"/> se acota a los que dan clases en ese club (el
    /// dueño siempre entra: trabaja en todos).
    /// </summary>
    Task<IReadOnlyList<ProfesorAsignableDto>> ListarAsignablesAsync(Guid? sedeId = null, CancellationToken ct = default);
    /// <summary>¿Ese usuario es asignable en este club? (el dueño o un staff activo).</summary>
    Task<bool> EsAsignableAsync(Guid userId, CancellationToken ct = default);

    /// <summary>La sede (club) del profe. Null si es el dueño o no tiene club.</summary>
    Task<Guid?> SedeDelProfeAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// ¿Ese profe da clases en ese club? El DUEÑO trabaja en todos, y un empleado sin
    /// club asignado también (todavía no se le fijó uno). Sin sede pedida, siempre sí.
    /// </summary>
    Task<bool> TrabajaEnSedeAsync(Guid userId, Guid? sedeId, CancellationToken ct = default);
    /// <summary>El propio profe empleado se da de baja del club (pasa a ser un usuario normal).</summary>
    Task DesvincularmeAsync(CancellationToken ct = default);
}

public class StaffService : IStaffService
{
    private readonly IMembresiaTenantRepository _membresias;
    private readonly ITenantRepository _tenants;
    private readonly ICredencialesService _credenciales;
    private readonly IUsuarioActual _usuario;
    private readonly IAlumnoRepository _alumnos;
    private readonly ISedeRepository _sedes;
    private readonly IPerfilProfesorService _perfiles;

    public StaffService(
        IMembresiaTenantRepository membresias, ITenantRepository tenants,
        ICredencialesService credenciales, IUsuarioActual usuario, IAlumnoRepository alumnos,
        ISedeRepository sedes, IPerfilProfesorService perfiles)
    {
        _membresias = membresias;
        _tenants = tenants;
        _credenciales = credenciales;
        _usuario = usuario;
        _alumnos = alumnos;
        _sedes = sedes;
        _perfiles = perfiles;
    }

    public async Task<IReadOnlyList<StaffDto>> ListarAsync(CancellationToken ct = default)
    {
        var lista = await _membresias.ListarConUsuarioAsync(ct);
        var sedes = (await _sedes.ListarAsync(ct)).ToDictionary(x => x.Id, x => x.Nombre);
        var equipo = new List<StaffDto>();

        // El Director va PRIMERO y como uno más del equipo: no tiene membresía, así
        // que se resuelve por Tenant.OwnerUserId y su ficha se arma a mano.
        var tenant = await _tenants.ObtenerActualAsync(ct);
        if (tenant.OwnerUserId is { } ownerId &&
            await _membresias.ObtenerUsuarioAsync(ownerId, ct) is { } dueño)
        {
            equipo.Add(new StaffDto
            {
                Id = Guid.Empty, // no hay membresía que identificar
                UserId = dueño.Id,
                Nombre = dueño.Nombre,
                Apellido = dueño.Apellido,
                Email = dueño.Email ?? string.Empty,
                Telefono = dueño.PhoneNumber ?? dueño.UserName ?? string.Empty,
                Dni = dueño.Dni,
                FechaNacimiento = dueño.FechaNacimiento,
                Activo = true, // el dueño no se da de baja de su propio club
                PuedeCobrar = true, // el dueño siempre puede cobrar, no se le puede sacar
                EsDueño = true,
                DaClases = tenant.DirectorDaClases,
                CreadoEl = tenant.CreadoEl,
            });
        }

        equipo.AddRange(lista.Select(x => Mapear(x.Membresia, x.Usuario, NombreSede(x.Membresia.SedeId, sedes))));
        return equipo;
    }

    public async Task EditarDirectorAsync(UpdateStaffDto dto, CancellationToken ct = default)
    {
        var tenant = await _tenants.ObtenerActualAsync(ct);
        var ownerId = tenant.OwnerUserId
            ?? throw new ReglaDeNegocioException("El club todavía no tiene un director asignado.");

        // Solo los datos personales: el director no tiene valor hora (no se paga
        // sueldo a sí mismo) ni club fijo (trabaja donde haga falta).
        var usuario = await _membresias.ObtenerUsuarioAsync(ownerId, ct)
            ?? throw new ReglaDeNegocioException("No se encontró la cuenta del director.");

        usuario.Nombre = dto.Nombre.Trim();
        usuario.Apellido = dto.Apellido.Trim();
        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        usuario.Email = email;
        usuario.NormalizedEmail = email?.ToUpperInvariant(); // Identity busca por el normalizado
        usuario.Dni = string.IsNullOrWhiteSpace(dto.Dni) ? null : dto.Dni.Trim();
        usuario.FechaNacimiento = dto.FechaNacimiento;

        await _membresias.GuardarCambiosAsync(ct);
    }

    /// <summary>Nombre de la sede desde el diccionario cargado (null si sin sede/desconocida).</summary>
    private static string? NombreSede(Guid? sedeId, IReadOnlyDictionary<Guid, string> sedes) =>
        sedeId is { } id && sedes.TryGetValue(id, out var n) ? n : null;

    /// <summary>Valida que la sede (si viene) sea del tenant; devuelve la entidad para el nombre.</summary>
    private async Task<Sede?> ValidarSedeAsync(Guid? sedeId, CancellationToken ct)
    {
        if (sedeId is not { } id) return null;
        return await _sedes.ObtenerAsync(id, ct) // el repo scopea por tenant → null si es de otro
            ?? throw new ReglaDeNegocioException("La sede no existe en tu academia.");
    }

    public async Task<StaffCreadoDto> AgregarAsync(AgregarStaffDto dto, CancellationToken ct = default)
    {
        var telefono = dto.Telefono.Trim();
        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        var tenant = await _tenants.ObtenerActualAsync(ct);
        var sede = await ValidarSedeAsync(dto.SedeId, ct); // el club donde va a trabajar (opcional)

        // ¿Ya hay una cuenta con ese celular? (el dueño, un ex-staff, u otro usuario)
        var existente = await _membresias.BuscarUsuarioPorTelefonoAsync(telefono, ct);
        if (existente is not null)
        {
            if (existente.Id == tenant.OwnerUserId)
                throw new ReglaDeNegocioException("Ese celular es el tuyo, el dueño de la academia.");

            var membresia = await _membresias.ObtenerPorUserIdAsync(existente.Id, ct);
            if (membresia is not null)
            {
                if (membresia.Activo)
                    throw new ReglaDeNegocioException("Ese profe ya está en tu equipo.");
                // Ya tuvo cuenta de profe acá y quedó inactivo: se reactiva (sin recrear ni nueva clave)
                membresia.Activo = true;
                if (dto.ValorHora is not null) membresia.ValorHora = dto.ValorHora;
                membresia.SedeId = dto.SedeId;
                await _membresias.GuardarCambiosAsync(ct);
                return new StaffCreadoDto { Staff = Mapear(membresia, existente, sede?.Nombre), Usuario = null, PasswordTemporal = null };
            }

            // Existe una cuenta con ese celular pero no es (ni fue) profe acá.
            // Si esa persona YA es alumno de esta academia, es la misma persona con
            // otra faceta (el empleado que también toma clases): la sumamos como Staff
            // reusando su login (un usuario, varios roles), sin crear otra cuenta ni
            // clave. Una cuenta ajena sin relación con el club NO se toca.
            if (await _alumnos.EsAlumnoDelTenantAsync(existente.Id, ct))
            {
                var reuso = new MembresiaTenant { UserId = existente.Id, Rol = RolTenant.Staff, ValorHora = dto.ValorHora, SedeId = dto.SedeId };
                await _membresias.AgregarAsync(reuso, ct);
                await _membresias.GuardarCambiosAsync(ct);
                return new StaffCreadoDto { Staff = Mapear(reuso, existente, sede?.Nombre), Usuario = null, PasswordTemporal = null };
            }

            throw new ReglaDeNegocioException(
                $"El celular {telefono} ya tiene una cuenta en la plataforma. Creá el profe con otro número, así tiene su cuenta propia de profesor.");
        }

        // No existe: le creamos la cuenta con el celular como usuario y clave temporal
        var cred = await _credenciales.CrearConTemporalAsync(
            telefono, dto.Nombre, dto.Apellido, dni: null, email: email, ct);

        var nueva = new MembresiaTenant { UserId = cred.UserId, Rol = RolTenant.Staff, ValorHora = dto.ValorHora, SedeId = dto.SedeId };
        await _membresias.AgregarAsync(nueva, ct);
        await _membresias.GuardarCambiosAsync(ct);

        var staff = new StaffDto
        {
            Id = nueva.Id,
            UserId = cred.UserId,
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Email = email ?? string.Empty,
            Telefono = telefono,
            Activo = true,
            ValorHora = dto.ValorHora,
            SedeId = dto.SedeId,
            SedeNombre = sede?.Nombre,
            CreadoEl = nueva.CreadoEl,
        };
        return new StaffCreadoDto { Staff = staff, Usuario = cred.PasswordTemporal, PasswordTemporal = cred.PasswordTemporal };
    }

    public async Task CambiarActivoAsync(Guid id, bool activo, CancellationToken ct = default)
    {
        var membresia = await _membresias.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("Ese profe no está en tu equipo.");

        membresia.Activo = activo;
        await _membresias.GuardarCambiosAsync(ct);
    }

    public async Task EliminarDefinitivoAsync(Guid id, CancellationToken ct = default)
    {
        var membresia = await _membresias.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("Ese profe no está en tu equipo.");

        var userId = membresia.UserId;

        // Su carta de presentación en ESTE club se va con él, fotos incluidas
        // (si da clases en otro club, ese perfil no se toca).
        await _perfiles.EliminarPerfilDeUsuarioAsync(userId, ct);

        // Saca la membresía y libera lo que lo tenía de profe (queda sin asignar).
        await _membresias.EliminarConReferenciasAsync(membresia, ct);

        // El login se va solo si no cumple otro rol (no es alumno, ni dueño, ni
        // staff de otro club). Si la persona además juega/entrena, su cuenta queda.
        if (!await _membresias.TieneOtrosRolesAsync(userId, ct))
            await _credenciales.EliminarAsync(userId, ct);
    }

    public async Task<IReadOnlyList<ProfesorAsignableDto>> ListarAsignablesAsync(
        Guid? sedeId = null, CancellationToken ct = default)
    {
        var tenant = await _tenants.ObtenerActualAsync(ct);
        var lista = new List<ProfesorAsignableDto>();

        // El dueño (Director) primero, PERO solo si da clases: si marcó que no,
        // deja de ofrecerse como profe asignable (solo gestiona la academia).
        if (tenant.DirectorDaClases && tenant.OwnerUserId is { } ownerId &&
            await _membresias.ObtenerUsuarioAsync(ownerId, ct) is { } dueño)
        {
            lista.Add(new ProfesorAsignableDto
            {
                UserId = dueño.Id,
                Nombre = $"{dueño.Nombre} {dueño.Apellido}",
                EsDueño = true,
            });
        }

        // Los staff ACTIVOS; si se pidió un club, solo los que dan clases ahí (los
        // que todavía no tienen club asignado entran igual).
        foreach (var (m, u) in await _membresias.ListarConUsuarioAsync(ct))
        {
            if (!m.Activo) continue;
            if (sedeId is { } sede && m.SedeId is not null && m.SedeId != sede) continue;
            lista.Add(new ProfesorAsignableDto
            {
                UserId = u.Id,
                Nombre = $"{u.Nombre} {u.Apellido}",
                EsDueño = false,
                ValorHora = m.ValorHora,
                SedeId = m.SedeId,
            });
        }

        return lista;
    }

    public async Task<bool> EsAsignableAsync(Guid userId, CancellationToken ct = default)
    {
        var tenant = await _tenants.ObtenerActualAsync(ct);
        if (tenant.OwnerUserId == userId) return true;
        var membresia = await _membresias.ObtenerPorUserIdAsync(userId, ct);
        return membresia is { Activo: true };
    }

    public async Task<Guid?> SedeDelProfeAsync(Guid userId, CancellationToken ct = default)
    {
        // El dueño no tiene membresía (trabaja en cualquier sede) → null.
        var membresia = await _membresias.ObtenerPorUserIdAsync(userId, ct);
        return membresia?.SedeId;
    }

    public async Task<bool> TrabajaEnSedeAsync(Guid userId, Guid? sedeId, CancellationToken ct = default)
    {
        if (sedeId is not { } sede) return true; // sin club pedido, cualquier profe sirve

        var tenant = await _tenants.ObtenerActualAsync(ct);
        if (tenant.OwnerUserId == userId) return true; // el director da clases donde haga falta

        var membresia = await _membresias.ObtenerPorUserIdAsync(userId, ct);
        // Un empleado sin club asignado todavía no está atado a ninguno: se acepta.
        return membresia is not null && (membresia.SedeId is null || membresia.SedeId == sede);
    }

    public async Task DesvincularmeAsync(CancellationToken ct = default)
    {
        var userId = _usuario.UserId
            ?? throw new ReglaDeNegocioException("No hay usuario en el contexto.");
        var membresia = await _membresias.ObtenerPorUserIdAsync(userId, ct);
        if (membresia is null || !membresia.Activo)
            throw new ReglaDeNegocioException("No sos parte de este club.");

        // Baja lógica: pasa a ser un usuario normal (el dueño lo puede reactivar).
        membresia.Activo = false;
        await _membresias.GuardarCambiosAsync(ct);
    }

    public async Task CambiarValorHoraAsync(Guid id, decimal? valorHora, CancellationToken ct = default)
    {
        var membresia = await _membresias.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("Ese profe no está en tu equipo.");

        if (valorHora is < 0)
            throw new ReglaDeNegocioException("El valor hora no puede ser negativo.");

        membresia.ValorHora = valorHora;
        await _membresias.GuardarCambiosAsync(ct);
    }

    public async Task EditarAsync(Guid id, UpdateStaffDto dto, CancellationToken ct = default)
    {
        var membresia = await _membresias.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("Ese profe no está en tu equipo.");

        if (dto.ValorHora is < 0)
            throw new ReglaDeNegocioException("El valor hora no puede ser negativo.");

        await ValidarSedeAsync(dto.SedeId, ct); // el club, si viene, tiene que ser del tenant

        // Los datos personales viven en el Usuario (identidad global). El celular
        // (login) NO se toca acá. La entidad viene trackeada → persiste con Guardar.
        var usuario = await _membresias.ObtenerUsuarioAsync(membresia.UserId, ct)
            ?? throw new ReglaDeNegocioException("No se encontró la cuenta del profe.");

        usuario.Nombre = dto.Nombre.Trim();
        usuario.Apellido = dto.Apellido.Trim();
        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        usuario.Email = email;
        usuario.NormalizedEmail = email?.ToUpperInvariant(); // Identity busca por el normalizado
        usuario.Dni = string.IsNullOrWhiteSpace(dto.Dni) ? null : dto.Dni.Trim();
        usuario.FechaNacimiento = dto.FechaNacimiento;

        membresia.ValorHora = dto.ValorHora; // el valor hora es del tenant (membresía)
        membresia.SedeId = dto.SedeId;       // el club donde trabaja
        membresia.PuedeCobrar = dto.PuedeCobrar; // Bloque 6, pedido 2

        await _membresias.GuardarCambiosAsync(ct);
    }

    private static StaffDto Mapear(MembresiaTenant m, Usuario u, string? sedeNombre = null) => new()
    {
        Id = m.Id,
        UserId = m.UserId,
        SedeId = m.SedeId,
        SedeNombre = sedeNombre,
        Nombre = u.Nombre,
        Apellido = u.Apellido,
        Email = u.Email ?? string.Empty,
        Telefono = u.PhoneNumber ?? u.UserName ?? string.Empty,
        Dni = u.Dni,
        FechaNacimiento = u.FechaNacimiento,
        Activo = m.Activo,
        PuedeCobrar = m.PuedeCobrar,
        ValorHora = m.ValorHora,
        CreadoEl = m.CreadoEl,
    };
}
