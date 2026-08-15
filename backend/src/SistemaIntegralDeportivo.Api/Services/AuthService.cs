using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class AuthService : IAuthService
{
    private readonly IAlumnoRepository _alumnos;
    private readonly ITenantRepository _tenants;
    private readonly ITokenService _tokens;
    private readonly ISedeRepository _sedes;
    private readonly ITenantActual _tenantActual;
    private readonly IMembresiaTenantRepository _membresias;

    public AuthService(
        IAlumnoRepository alumnos, ITenantRepository tenants, ITokenService tokens,
        ISedeRepository sedes, ITenantActual tenantActual, IMembresiaTenantRepository membresias)
    {
        _alumnos = alumnos;
        _tenants = tenants;
        _tokens = tokens;
        _sedes = sedes;
        _tenantActual = tenantActual;
        _membresias = membresias;
    }

    public async Task<SesionDto> ArmarSesionAsync(
        Usuario usuario, bool incluirToken, CancellationToken ct = default)
    {
        // El tenant en el que trabaja (dueño o staff) viaja en el token: los repos
        // de gestión operan ESE club (ADR-0010). Prioridad: dueño → staff → jugador.
        // Un tenant pendiente de pago NO habilita la gestión: primero el checkout.
        var tenantPropio = await _tenants.ObtenerPorOwnerAsync(usuario.Id, ct);

        Tenant? tenantDeTrabajo = null;
        RolTenant? rol = null;
        Guid? sedeStaff = null; // el club del empleado (el dueño no tiene sede fija)
        var puedeCobrar = false; // el dueño la resuelve la policy por rol=owner; esto es solo para el staff
        var estadoTenant = tenantPropio?.Estado.ToString();

        if (tenantPropio?.Estado == EstadoTenant.Activo)
        {
            // Dueño (head pro) de un club activo
            tenantDeTrabajo = tenantPropio;
            rol = RolTenant.Dueño;
        }
        else if (tenantPropio is null)
        {
            // No es dueño: ¿es profe EMPLEADO (Staff) de alguna academia activa?
            var membresia = await _membresias.ObtenerActivaPorUserIdAsync(usuario.Id, ct);
            if (membresia is not null)
            {
                var academia = await _tenants.ObtenerPorIdAsync(membresia.TenantId, ct);
                if (academia?.Estado == EstadoTenant.Activo)
                {
                    tenantDeTrabajo = academia;
                    rol = RolTenant.Staff;
                    estadoTenant = academia.Estado.ToString();
                    sedeStaff = membresia.SedeId; // su club, para acotarle las canchas
                    puedeCobrar = membresia.PuedeCobrar;
                }
            }
        }

        var esProfesor = tenantDeTrabajo is not null;

        // Capa 2 (cuenta familiar): un titular puede tener VARIAS fichas (sus hijos).
        // Alumno = la default (compatibilidad); Alumnos = toda la familia.
        var fichas = await _alumnos.ListarPorUserIdAsync(usuario.Id, ct);

        // "Está en la lista de espera" ya no es una columna: es no tener clase. Un
        // query para toda la familia, que puede tener fichas en clubes distintos.
        var conClase = await _alumnos.FiltrarConClaseAsync([.. fichas.Select(f => f.Id)], ct);

        return new SesionDto
        {
            Token = incluirToken ? _tokens.Generar(usuario, tenantDeTrabajo, rol, puedeCobrar) : null,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email ?? string.Empty,
            EsProfesor = esProfesor,
            Rol = rol switch
            {
                RolTenant.Dueño => "owner",
                RolTenant.Staff => "staff",
                _ => null,
            },
            EsAdmin = usuario.EsAdminPlataforma,
            EstadoTenant = estadoTenant,
            SedeId = sedeStaff,
            // El dueño siempre puede cobrar (no necesita el flag); el staff, solo si se lo habilitaron.
            PuedeCobrar = rol == RolTenant.Dueño || puedeCobrar,
            DebeCambiarPassword = usuario.DebeCambiarPassword,
            Dni = usuario.Dni,
            Telefono = usuario.PhoneNumber,
            FechaNacimiento = usuario.FechaNacimiento,
            Categoria = usuario.Categoria?.ToString(),
            Alumno = fichas.Count == 0 ? null : Mapear(fichas[0], conClase),
            Alumnos = fichas.Select(f => Mapear(f, conClase)).ToList(),
        };
    }

    public async Task<Tenant> CrearTenantParaAsync(
        Usuario usuario, string nombreClub, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nombreClub))
            throw new ReglaDeNegocioException("Poné el nombre de tu club o academia.");

        if (await _tenants.ObtenerPorOwnerAsync(usuario.Id, ct) is not null)
            throw new ReglaDeNegocioException("Ya tenés un club registrado en la plataforma.");

        // Subdominio = slug del nombre; si colisiona, sufijo -2, -3…
        var baseSlug = Slug(nombreClub);
        if (baseSlug.Length == 0)
            throw new ReglaDeNegocioException("El nombre del club necesita al menos una letra o número.");
        var slug = baseSlug;
        for (var i = 2; await _tenants.ExisteSubdominioAsync(slug, ct); i++)
            slug = $"{baseSlug}-{i}";

        var tenant = new Tenant
        {
            Subdominio = slug,
            Nombre = nombreClub.Trim(),
            Tipo = TipoTenant.Profesor,
            Estado = EstadoTenant.PendientePago, // se activa al pagar
            OwnerUserId = usuario.Id,
        };
        await _tenants.AgregarAsync(tenant, ct);
        await _tenants.GuardarCambiosAsync(ct);

        // La primera sede nace con el club (caso típico: un solo lugar);
        // se renombra o amplía en Configuración. El repo de sedes scopea
        // por ITenantActual → fijar el club recién creado ANTES.
        _tenantActual.Establecer(tenant.Id);
        await _sedes.AgregarAsync(new Sede { Nombre = tenant.Nombre }, ct);

        return tenant;
    }

    public async Task<SesionDto> ActivarTenantAsync(Usuario usuario, CancellationToken ct = default)
    {
        // Por owner y NO por claim: el tenant pendiente todavía no emite claim
        var tenant = await _tenants.ObtenerPorOwnerAsync(usuario.Id, ct)
            ?? throw new ReglaDeNegocioException("No tenés ningún club para activar.");

        // Idempotente: el webhook de MP puede reintentar la notificación
        if (tenant.Estado != EstadoTenant.Activo)
        {
            tenant.Estado = EstadoTenant.Activo;
            await _tenants.GuardarCambiosAsync(ct);
        }

        return await ArmarSesionAsync(usuario, incluirToken: true, ct);
    }

    /// <summary>"Academia Río Cuarto" → "academia-rio-cuarto" (minúsculas, sin acentos).</summary>
    private static string Slug(string nombre)
    {
        var sinAcentos = new string(nombre
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray());

        var slug = new System.Text.StringBuilder();
        foreach (var c in sinAcentos.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(c)) slug.Append(c);
            else if (slug.Length > 0 && slug[^1] != '-') slug.Append('-');
        }
        return slug.ToString().Trim('-');
    }

    private static FichaDto Mapear(Alumno a, HashSet<Guid> conClase) => new()
    {
        AlumnoId = a.Id,
        TenantId = a.TenantId,
        Nombre = a.Nombre,
        Apellido = a.Apellido,
        Club = a.Tenant?.Nombre ?? string.Empty,
        EnEspera = !conClase.Contains(a.Id),
    };
}
