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

    public AuthService(
        IAlumnoRepository alumnos, ITenantRepository tenants, ITokenService tokens,
        ISedeRepository sedes, ITenantActual tenantActual)
    {
        _alumnos = alumnos;
        _tenants = tenants;
        _tokens = tokens;
        _sedes = sedes;
        _tenantActual = tenantActual;
    }

    public async Task<SesionDto> ArmarSesionAsync(
        Usuario usuario, bool incluirToken, CancellationToken ct = default)
    {
        // El tenant que administra (si es dueño Y está ACTIVO) viaja en el
        // token: los repos de gestión operan ESE club (ADR-0010). Un tenant
        // pendiente de pago NO habilita la gestión: primero el checkout.
        var tenantPropio = await _tenants.ObtenerPorOwnerAsync(usuario.Id, ct);
        var esProfesor = tenantPropio?.Estado == EstadoTenant.Activo;

        // Una ficha por usuario en el prototipo (multi-membresía: fase futura).
        // Si ya reclamó una, no se ofrecen más.
        var vinculada = await _alumnos.ObtenerPorUserIdAsync(usuario.Id, ct);
        var porReclamar = vinculada is null
            ? await CandidatasAsync(usuario, ct)
            : [];

        return new SesionDto
        {
            Token = incluirToken ? _tokens.Generar(usuario, esProfesor ? tenantPropio : null) : null,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email ?? string.Empty,
            EsProfesor = esProfesor,
            EstadoTenant = tenantPropio?.Estado.ToString(),
            Alumno = vinculada is null ? null : Mapear(vinculada),
            FichasPorReclamar = porReclamar.Select(Mapear).ToList(),
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

    public async Task ReclamarFichaAsync(
        Usuario usuario, Guid alumnoId, CancellationToken ct = default)
    {
        // La ficha tiene que estar entre MIS candidatas: libre (sin UserId)
        // y coincidente por DNI/teléfono. Todo lo demás es un reclamo inválido.
        var candidatas = await CandidatasAsync(usuario, ct);
        var ficha = candidatas.FirstOrDefault(a => a.Id == alumnoId)
            ?? throw new ReglaDeNegocioException(
                "La ficha no existe, ya fue reclamada o no coincide con tus datos (DNI/teléfono).");

        ficha.UserId = usuario.Id;
        await _alumnos.GuardarCambiosAsync(ct);
    }

    private async Task<IReadOnlyList<Alumno>> CandidatasAsync(Usuario usuario, CancellationToken ct)
    {
        // Sin DNI ni teléfono no hay contra qué matchear
        if (string.IsNullOrWhiteSpace(usuario.Dni) && string.IsNullOrWhiteSpace(usuario.PhoneNumber))
            return [];

        return await _alumnos.BuscarReclamablesAsync(usuario.Dni, usuario.PhoneNumber, ct);
    }

    private static FichaDto Mapear(Alumno a) => new()
    {
        AlumnoId = a.Id,
        Nombre = a.Nombre,
        Apellido = a.Apellido,
        Club = a.Tenant?.Nombre ?? string.Empty,
    };
}
