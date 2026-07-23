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

    /// <summary>Los profes a los que el dueño puede asignar clases: el dueño + los staff ACTIVOS.</summary>
    Task<IReadOnlyList<ProfesorAsignableDto>> ListarAsignablesAsync(CancellationToken ct = default);
    /// <summary>¿Ese usuario es asignable en este club? (el dueño o un staff activo).</summary>
    Task<bool> EsAsignableAsync(Guid userId, CancellationToken ct = default);
    /// <summary>El propio profe empleado se da de baja del club (pasa a ser un usuario normal).</summary>
    Task DesvincularmeAsync(CancellationToken ct = default);
}

public class StaffService : IStaffService
{
    private readonly IMembresiaTenantRepository _membresias;
    private readonly ITenantRepository _tenants;
    private readonly ICredencialesService _credenciales;
    private readonly IUsuarioActual _usuario;

    public StaffService(
        IMembresiaTenantRepository membresias, ITenantRepository tenants,
        ICredencialesService credenciales, IUsuarioActual usuario)
    {
        _membresias = membresias;
        _tenants = tenants;
        _credenciales = credenciales;
        _usuario = usuario;
    }

    public async Task<IReadOnlyList<StaffDto>> ListarAsync(CancellationToken ct = default)
    {
        var lista = await _membresias.ListarConUsuarioAsync(ct);
        return lista.Select(x => Mapear(x.Membresia, x.Usuario)).ToList();
    }

    public async Task<StaffCreadoDto> AgregarAsync(AgregarStaffDto dto, CancellationToken ct = default)
    {
        var telefono = dto.Telefono.Trim();
        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        var tenant = await _tenants.ObtenerActualAsync(ct);

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
                await _membresias.GuardarCambiosAsync(ct);
                return new StaffCreadoDto { Staff = Mapear(membresia, existente), Usuario = null, PasswordTemporal = null };
            }

            // Existe una cuenta con ese celular pero no es (ni fue) profe acá: no la pisamos
            throw new ReglaDeNegocioException(
                $"El celular {telefono} ya tiene una cuenta en la plataforma. Creá el profe con otro número, así tiene su cuenta propia de profesor.");
        }

        // No existe: le creamos la cuenta con el celular como usuario y clave temporal
        var cred = await _credenciales.CrearConTemporalAsync(
            telefono, dto.Nombre, dto.Apellido, dni: null, email: email, ct);

        var nueva = new MembresiaTenant { UserId = cred.UserId, Rol = RolTenant.Staff };
        await _membresias.AgregarAsync(nueva, ct);
        await _membresias.GuardarCambiosAsync(ct);

        var staff = new StaffDto
        {
            Id = nueva.Id,
            UserId = cred.UserId,
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Email = email ?? string.Empty,
            Activo = true,
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

    public async Task<IReadOnlyList<ProfesorAsignableDto>> ListarAsignablesAsync(CancellationToken ct = default)
    {
        var tenant = await _tenants.ObtenerActualAsync(ct);
        var lista = new List<ProfesorAsignableDto>();

        // El dueño primero (es un profe más a la hora de asignar clases)
        if (tenant.OwnerUserId is { } ownerId &&
            await _membresias.ObtenerUsuarioAsync(ownerId, ct) is { } dueño)
        {
            lista.Add(new ProfesorAsignableDto
            {
                UserId = dueño.Id,
                Nombre = $"{dueño.Nombre} {dueño.Apellido}",
                EsDueño = true,
            });
        }

        // Los staff ACTIVOS
        foreach (var (m, u) in await _membresias.ListarConUsuarioAsync(ct))
        {
            if (!m.Activo) continue;
            lista.Add(new ProfesorAsignableDto
            {
                UserId = u.Id,
                Nombre = $"{u.Nombre} {u.Apellido}",
                EsDueño = false,
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

    private static StaffDto Mapear(MembresiaTenant m, Usuario u) => new()
    {
        Id = m.Id,
        UserId = m.UserId,
        Nombre = u.Nombre,
        Apellido = u.Apellido,
        Email = u.Email ?? string.Empty,
        Activo = m.Activo,
    };
}
