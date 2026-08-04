using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class PerfilProfesorService : IPerfilProfesorService
{
    /// <summary>Techo de seguridad del servidor; el front igual comprime a ~200 KB antes de subir.</summary>
    private const int TamañoMaximoBytes = 5 * 1024 * 1024;
    private const int MaximoFotos = 12;
    private const int MaximoHitos = 15;
    private const int MaximoEspecialidades = 8;
    private const int LargoEspecialidad = 30;
    private const int LargoTitular = 80;
    private const int LargoSubtitulo = 120;
    private const int LargoBio = 2000;
    private const int LargoPieDeFoto = 120;
    private const int LargoTituloHito = 120;
    private const int LargoDetalleHito = 400;
    private const int AnioMinimo = 1950;

    private readonly IPerfilProfesorRepository _perfiles;
    private readonly IAlmacenamientoArchivos _archivos;
    private readonly ITenantActual _tenantActual;
    private readonly IUsuarioActual _usuarioActual;
    private readonly ITenantRepository _tenants;
    private readonly IMembresiaTenantRepository _membresias;
    private readonly ILogger<PerfilProfesorService> _log;

    public PerfilProfesorService(
        IPerfilProfesorRepository perfiles,
        IAlmacenamientoArchivos archivos,
        ITenantActual tenantActual,
        IUsuarioActual usuarioActual,
        ITenantRepository tenants,
        IMembresiaTenantRepository membresias,
        ILogger<PerfilProfesorService> log)
    {
        _perfiles = perfiles;
        _archivos = archivos;
        _tenantActual = tenantActual;
        _usuarioActual = usuarioActual;
        _tenants = tenants;
        _membresias = membresias;
        _log = log;
    }

    private Guid UserId => _usuarioActual.UserId
        ?? throw new InvalidOperationException("Request sin usuario: el endpoint tendría que exigir autenticación.");

    // ── Lo mío ──

    public async Task<MiPerfilProfesorDto> ObtenerMioAsync(CancellationToken ct = default)
    {
        var perfil = await _perfiles.ObtenerDeUsuarioAsync(UserId, ct);
        return await MapearMioAsync(perfil, ct);
    }

    public async Task<MiPerfilProfesorDto> GuardarMioAsync(GuardarPerfilProfesorDto dto, CancellationToken ct = default)
    {
        var titular = Recortar(dto.Titular);
        var subtitulo = Recortar(dto.Subtitulo);
        var bio = Recortar(dto.Bio);

        if (titular?.Length > LargoTitular)
            throw new ReglaDeNegocioException($"El título es muy largo (máximo {LargoTitular} caracteres).");
        if (subtitulo?.Length > LargoSubtitulo)
            throw new ReglaDeNegocioException($"La frase de presentación es muy larga (máximo {LargoSubtitulo} caracteres).");
        if (bio?.Length > LargoBio)
            throw new ReglaDeNegocioException($"El texto de \"Quién soy\" es muy largo (máximo {LargoBio} caracteres).");

        var especialidades = NormalizarEspecialidades(dto.Especialidades);

        var perfil = await ObtenerOCrearAsync(ct);
        perfil.Titular = titular;
        perfil.Subtitulo = subtitulo;
        perfil.Bio = bio;
        perfil.Especialidades = especialidades;
        perfil.Publicado = dto.Publicado;
        perfil.ActualizadoEl = DateTime.UtcNow;

        await _perfiles.GuardarCambiosAsync(ct);
        return await MapearMioAsync(perfil, ct);
    }

    /// <summary>Trim, sin vacías y sin repetir (comparando sin distinguir mayúsculas).</summary>
    private static List<string> NormalizarEspecialidades(IEnumerable<string> especialidades)
    {
        var limpias = new List<string>();
        foreach (var especialidad in especialidades)
        {
            var texto = especialidad?.Trim();
            if (string.IsNullOrEmpty(texto)) continue;
            if (texto.Length > LargoEspecialidad)
                throw new ReglaDeNegocioException($"\"{texto}\" es muy largo para una especialidad (máximo {LargoEspecialidad} caracteres).");
            if (limpias.Any(x => x.Equals(texto, StringComparison.OrdinalIgnoreCase))) continue;
            limpias.Add(texto);
        }

        if (limpias.Count > MaximoEspecialidades)
            throw new ReglaDeNegocioException($"Podés mostrar hasta {MaximoEspecialidades} especialidades.");

        return limpias;
    }

    public async Task<ImagenSubidaDto> SubirPortadaAsync(ImagenSubida imagen, CancellationToken ct = default)
    {
        var perfil = await ObtenerOCrearAsync(ct);
        var rutaVieja = perfil.PortadaRuta;

        var (url, ruta) = await GuardarImagenAsync(imagen, "portada", ct);
        perfil.PortadaUrl = url;
        perfil.PortadaRuta = ruta;
        perfil.ActualizadoEl = DateTime.UtcNow;
        await _perfiles.GuardarCambiosAsync(ct);

        await BorrarArchivoAsync(rutaVieja, ct);
        return new ImagenSubidaDto { Url = url };
    }

    public async Task<ImagenSubidaDto> SubirAvatarAsync(ImagenSubida imagen, CancellationToken ct = default)
    {
        var perfil = await ObtenerOCrearAsync(ct);
        var rutaVieja = perfil.AvatarRuta;

        var (url, ruta) = await GuardarImagenAsync(imagen, "avatar", ct);
        perfil.AvatarUrl = url;
        perfil.AvatarRuta = ruta;
        perfil.ActualizadoEl = DateTime.UtcNow;
        await _perfiles.GuardarCambiosAsync(ct);

        await BorrarArchivoAsync(rutaVieja, ct);
        return new ImagenSubidaDto { Url = url };
    }

    public async Task QuitarPortadaAsync(CancellationToken ct = default)
    {
        var perfil = await _perfiles.ObtenerDeUsuarioAsync(UserId, ct);
        if (perfil?.PortadaRuta is null) return;

        var ruta = perfil.PortadaRuta;
        perfil.PortadaUrl = null;
        perfil.PortadaRuta = null;
        perfil.ActualizadoEl = DateTime.UtcNow;
        await _perfiles.GuardarCambiosAsync(ct);

        await BorrarArchivoAsync(ruta, ct);
    }

    public async Task QuitarAvatarAsync(CancellationToken ct = default)
    {
        var perfil = await _perfiles.ObtenerDeUsuarioAsync(UserId, ct);
        if (perfil?.AvatarRuta is null) return;

        var ruta = perfil.AvatarRuta;
        perfil.AvatarUrl = null;
        perfil.AvatarRuta = null;
        perfil.ActualizadoEl = DateTime.UtcNow;
        await _perfiles.GuardarCambiosAsync(ct);

        await BorrarArchivoAsync(ruta, ct);
    }

    // ── Galería ──

    public async Task<FotoPerfilDto> AgregarFotoAsync(ImagenSubida imagen, string? pieDeFoto, CancellationToken ct = default)
    {
        var pie = Recortar(pieDeFoto);
        if (pie?.Length > LargoPieDeFoto)
            throw new ReglaDeNegocioException($"El pie de foto es muy largo (máximo {LargoPieDeFoto} caracteres).");

        var perfil = await ObtenerOCrearAsync(ct);
        if (perfil.Fotos.Count >= MaximoFotos)
            throw new ReglaDeNegocioException($"Ya tenés {MaximoFotos} fotos: borrá alguna para subir otra.");

        var (url, ruta) = await GuardarImagenAsync(imagen, "galeria", ct);
        var foto = new FotoPerfil
        {
            Url = url,
            Ruta = ruta,
            PieDeFoto = pie,
            Orden = perfil.Fotos.Count == 0 ? 0 : perfil.Fotos.Max(f => f.Orden) + 1,
        };
        _perfiles.AgregarFoto(foto);
        perfil.Fotos.Add(foto);
        perfil.ActualizadoEl = DateTime.UtcNow;
        await _perfiles.GuardarCambiosAsync(ct);

        return Mapear(foto);
    }

    public async Task CambiarPieDeFotoAsync(Guid fotoId, string? pieDeFoto, CancellationToken ct = default)
    {
        var pie = Recortar(pieDeFoto);
        if (pie?.Length > LargoPieDeFoto)
            throw new ReglaDeNegocioException($"El pie de foto es muy largo (máximo {LargoPieDeFoto} caracteres).");

        var perfil = await ObtenerMioObligatorioAsync(ct);
        var foto = perfil.Fotos.FirstOrDefault(f => f.Id == fotoId)
            ?? throw new ReglaDeNegocioException("La foto no existe.");

        foto.PieDeFoto = pie;
        perfil.ActualizadoEl = DateTime.UtcNow;
        await _perfiles.GuardarCambiosAsync(ct);
    }

    public async Task EliminarFotoAsync(Guid fotoId, CancellationToken ct = default)
    {
        var perfil = await ObtenerMioObligatorioAsync(ct);
        var foto = perfil.Fotos.FirstOrDefault(f => f.Id == fotoId)
            ?? throw new ReglaDeNegocioException("La foto no existe.");

        _perfiles.EliminarFoto(foto);
        perfil.ActualizadoEl = DateTime.UtcNow;
        await _perfiles.GuardarCambiosAsync(ct);

        await BorrarArchivoAsync(foto.Ruta, ct);
    }

    public async Task ReordenarFotosAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        var perfil = await ObtenerMioObligatorioAsync(ct);
        var fotos = perfil.Fotos.ToList();
        ValidarReordenamiento(ids, fotos.Select(f => f.Id));

        for (var i = 0; i < ids.Count; i++)
            fotos.First(f => f.Id == ids[i]).Orden = i;

        perfil.ActualizadoEl = DateTime.UtcNow;
        await _perfiles.GuardarCambiosAsync(ct);
    }

    // ── Trayectoria ──

    public async Task<HitoTrayectoriaDto> AgregarHitoAsync(GuardarHitoDto dto, CancellationToken ct = default)
    {
        var (anio, titulo, detalle) = ValidarHito(dto);

        var perfil = await ObtenerOCrearAsync(ct);
        if (perfil.Hitos.Count >= MaximoHitos)
            throw new ReglaDeNegocioException($"Podés cargar hasta {MaximoHitos} hitos en tu trayectoria.");

        var hito = new HitoTrayectoria
        {
            Anio = anio,
            Titulo = titulo,
            Detalle = detalle,
            Orden = perfil.Hitos.Count == 0 ? 0 : perfil.Hitos.Max(h => h.Orden) + 1,
        };
        _perfiles.AgregarHito(hito);
        perfil.Hitos.Add(hito);
        perfil.ActualizadoEl = DateTime.UtcNow;
        await _perfiles.GuardarCambiosAsync(ct);

        return Mapear(hito);
    }

    public async Task EditarHitoAsync(Guid hitoId, GuardarHitoDto dto, CancellationToken ct = default)
    {
        var (anio, titulo, detalle) = ValidarHito(dto);

        var perfil = await ObtenerMioObligatorioAsync(ct);
        var hito = perfil.Hitos.FirstOrDefault(h => h.Id == hitoId)
            ?? throw new ReglaDeNegocioException("El hito no existe.");

        hito.Anio = anio;
        hito.Titulo = titulo;
        hito.Detalle = detalle;
        perfil.ActualizadoEl = DateTime.UtcNow;
        await _perfiles.GuardarCambiosAsync(ct);
    }

    public async Task EliminarHitoAsync(Guid hitoId, CancellationToken ct = default)
    {
        var perfil = await ObtenerMioObligatorioAsync(ct);
        var hito = perfil.Hitos.FirstOrDefault(h => h.Id == hitoId)
            ?? throw new ReglaDeNegocioException("El hito no existe.");

        _perfiles.EliminarHito(hito);
        perfil.ActualizadoEl = DateTime.UtcNow;
        await _perfiles.GuardarCambiosAsync(ct);
    }

    public async Task ReordenarHitosAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        var perfil = await ObtenerMioObligatorioAsync(ct);
        var hitos = perfil.Hitos.ToList();
        ValidarReordenamiento(ids, hitos.Select(h => h.Id));

        for (var i = 0; i < ids.Count; i++)
            hitos.First(h => h.Id == ids[i]).Orden = i;

        perfil.ActualizadoEl = DateTime.UtcNow;
        await _perfiles.GuardarCambiosAsync(ct);
    }

    private static (int Anio, string Titulo, string? Detalle) ValidarHito(GuardarHitoDto dto)
    {
        var titulo = Recortar(dto.Titulo);
        if (string.IsNullOrEmpty(titulo))
            throw new ReglaDeNegocioException("El hito necesita un título.");
        if (titulo.Length > LargoTituloHito)
            throw new ReglaDeNegocioException($"El título del hito es muy largo (máximo {LargoTituloHito} caracteres).");

        var detalle = Recortar(dto.Detalle);
        if (detalle?.Length > LargoDetalleHito)
            throw new ReglaDeNegocioException($"El detalle del hito es muy largo (máximo {LargoDetalleHito} caracteres).");

        // El año que viene se acepta (un torneo ya programado); más allá, es un error de tipeo
        if (dto.Anio < AnioMinimo || dto.Anio > DateTime.UtcNow.Year + 1)
            throw new ReglaDeNegocioException($"El año tiene que estar entre {AnioMinimo} y {DateTime.UtcNow.Year + 1}.");

        return (dto.Anio, titulo, detalle);
    }

    /// <summary>El nuevo orden tiene que ser exactamente los mismos ids: ni de más, ni de menos.</summary>
    private static void ValidarReordenamiento(IReadOnlyList<Guid> ids, IEnumerable<Guid> actuales)
    {
        var esperados = actuales.ToHashSet();
        if (ids.Count != esperados.Count || ids.Distinct().Count() != ids.Count || !ids.All(esperados.Contains))
            throw new ReglaDeNegocioException("El orden que llegó no coincide con lo que hay cargado: recargá la página.");
    }

    // ── Borrado del perfil completo (lo llama la baja definitiva de un empleado) ──

    public async Task EliminarPerfilDeUsuarioAsync(Guid userId, CancellationToken ct = default)
    {
        var perfil = await _perfiles.ObtenerDeUsuarioAsync(userId, ct);
        if (perfil is null) return;

        var rutas = perfil.Fotos.Select(f => f.Ruta)
            .Concat([perfil.AvatarRuta, perfil.PortadaRuta])
            .Where(r => !string.IsNullOrEmpty(r))
            .ToList();

        // La base primero: las filas hijas se van por cascada
        _perfiles.Eliminar(perfil);
        await _perfiles.GuardarCambiosAsync(ct);

        foreach (var ruta in rutas)
            await BorrarArchivoAsync(ruta, ct);
    }

    // ── Lo que ve el alumno ──

    public async Task<IReadOnlyList<ProfesorTarjetaDto>> ListarProfesoresDelClubAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _perfiles.ObtenerTenantAsync(tenantId, ct);
        if (tenant is null || tenant.Estado != EstadoTenant.Activo) return [];

        var profesores = await _perfiles.ListarProfesoresDelClubAsync(tenantId, ct);
        return profesores.Select(x => new ProfesorTarjetaDto
        {
            UserId = x.Usuario.Id,
            Nombre = x.Usuario.Nombre ?? string.Empty,
            Apellido = x.Usuario.Apellido ?? string.Empty,
            EsDueño = x.EsDueño,
            // Los datos del perfil solo si está publicado: si no, la tarjeta muestra
            // el nombre y nada más (el alumno igual necesita saber quién le da clases)
            Titular = x.Perfil?.Publicado == true ? x.Perfil.Titular : null,
            AvatarUrl = x.Perfil?.Publicado == true ? x.Perfil.AvatarUrl : null,
            Especialidades = x.Perfil?.Publicado == true ? x.Perfil.Especialidades : [],
            TienePerfil = x.Perfil?.Publicado == true,
        }).ToList();
    }

    public async Task<PerfilProfesorPublicoDto?> ObtenerPublicoAsync(
        Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        var tenant = await _perfiles.ObtenerTenantAsync(tenantId, ct);
        if (tenant is null || tenant.Estado != EstadoTenant.Activo) return null;

        // Un profe que se fue del club deja de mostrarse ahí, aunque su perfil siga cargado
        if (!await _perfiles.TrabajaEnElClubAsync(tenantId, userId, ct)) return null;

        var encontrado = await _perfiles.ObtenerDeClubAsync(tenantId, userId, ct);
        if (encontrado is null) return null;

        var (usuario, perfil) = encontrado.Value;
        if (!perfil.Publicado) return null;

        return new PerfilProfesorPublicoDto
        {
            UserId = usuario.Id,
            Nombre = usuario.Nombre ?? string.Empty,
            Apellido = usuario.Apellido ?? string.Empty,
            Club = tenant.Nombre,
            Titular = perfil.Titular,
            Subtitulo = perfil.Subtitulo,
            Bio = perfil.Bio,
            Especialidades = perfil.Especialidades,
            PortadaUrl = perfil.PortadaUrl,
            AvatarUrl = perfil.AvatarUrl,
            Fotos = [.. perfil.Fotos.OrderBy(f => f.Orden).Select(Mapear)],
            Hitos = [.. perfil.Hitos.OrderBy(h => h.Orden).Select(Mapear)],
        };
    }

    // ── Ayudantes ──

    private async Task<PerfilProfesor> ObtenerOCrearAsync(CancellationToken ct)
    {
        var perfil = await _perfiles.ObtenerDeUsuarioAsync(UserId, ct);
        if (perfil is not null) return perfil;

        // Se crea recién cuando el profe carga algo: hasta entonces no ocupa una fila
        perfil = new PerfilProfesor { UserId = UserId };
        await _perfiles.AgregarAsync(perfil, ct); // el repositorio le pone el TenantId
        return perfil;
    }

    private async Task<PerfilProfesor> ObtenerMioObligatorioAsync(CancellationToken ct) =>
        await _perfiles.ObtenerDeUsuarioAsync(UserId, ct)
        ?? throw new ReglaDeNegocioException("Todavía no tenés un perfil cargado.");

    /// <summary>
    /// Valida los BYTES (no lo que declara el cliente) y guarda el archivo bajo la
    /// carpeta del tenant y del profe. La ruta la arma siempre el backend: el cliente
    /// no elige ni el nombre ni la carpeta.
    /// </summary>
    private async Task<(string Url, string Ruta)> GuardarImagenAsync(ImagenSubida imagen, string tipo, CancellationToken ct)
    {
        if (imagen.Contenido.Length == 0)
            throw new ReglaDeNegocioException("El archivo llegó vacío: probá de nuevo.");
        if (imagen.Contenido.Length > TamañoMaximoBytes)
            throw new ReglaDeNegocioException("La imagen es muy pesada: probá con una más liviana (hasta 5 MB).");

        var contentType = TipoDeImagen.Detectar(imagen.Contenido)
            ?? throw new ReglaDeNegocioException("El archivo tiene que ser una imagen JPG, PNG o WEBP.");

        var ruta = $"perfiles/{_tenantActual.TenantId}/{UserId}/{tipo}-{Guid.NewGuid():N}.{TipoDeImagen.ExtensionDe(contentType)}";
        using var contenido = new MemoryStream(imagen.Contenido);
        var url = await _archivos.SubirAsync(contenido, contentType, ruta, ct);
        return (url, ruta);
    }

    /// <summary>
    /// El archivo se borra DESPUÉS de la base y sin cortar el request si falla: un
    /// huérfano en el storage no lo ve nadie, una foto rota en la pantalla sí.
    /// </summary>
    private async Task BorrarArchivoAsync(string? ruta, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(ruta)) return;
        try
        {
            await _archivos.EliminarAsync(ruta, ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Quedó un archivo huérfano en el storage: {Ruta}", ruta);
        }
    }

    private async Task<MiPerfilProfesorDto> MapearMioAsync(PerfilProfesor? perfil, CancellationToken ct)
    {
        var tenant = await _tenants.ObtenerActualAsync(ct);
        var usuario = await _membresias.ObtenerUsuarioAsync(UserId, ct);

        return new MiPerfilProfesorDto
        {
            Nombre = usuario?.Nombre ?? string.Empty,
            Apellido = usuario?.Apellido ?? string.Empty,
            Club = tenant.Nombre,
            Titular = perfil?.Titular,
            Subtitulo = perfil?.Subtitulo,
            Bio = perfil?.Bio,
            Especialidades = perfil?.Especialidades ?? [],
            PortadaUrl = perfil?.PortadaUrl,
            AvatarUrl = perfil?.AvatarUrl,
            Publicado = perfil?.Publicado ?? false,
            Fotos = perfil is null ? [] : [.. perfil.Fotos.OrderBy(f => f.Orden).Select(Mapear)],
            Hitos = perfil is null ? [] : [.. perfil.Hitos.OrderBy(h => h.Orden).Select(Mapear)],
        };
    }

    private static string? Recortar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static FotoPerfilDto Mapear(FotoPerfil f) => new()
    {
        Id = f.Id,
        Url = f.Url,
        PieDeFoto = f.PieDeFoto,
        Orden = f.Orden,
    };

    private static HitoTrayectoriaDto Mapear(HitoTrayectoria h) => new()
    {
        Id = h.Id,
        Anio = h.Anio,
        Titulo = h.Titulo,
        Detalle = h.Detalle,
        Orden = h.Orden,
    };
}
