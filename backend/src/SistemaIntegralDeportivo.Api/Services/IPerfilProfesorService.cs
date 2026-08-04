using SistemaIntegralDeportivo.Api.Dtos;

namespace SistemaIntegralDeportivo.Api.Services;

/// <summary>
/// Una imagen ya leída del request. El Service no conoce IFormFile (eso es HTTP y
/// vive en el controller): acá llegan los bytes, que además son lo único con lo que
/// se puede verificar de verdad que sea una imagen.
/// </summary>
public record ImagenSubida(byte[] Contenido);

/// <summary>
/// El perfil público del profe: lo edita su dueño (el propio profe, sea el director
/// o un empleado) y lo leen los alumnos del club.
/// </summary>
public interface IPerfilProfesorService
{
    // ── Lo mío (profe autenticado, tenant del token) ──

    /// <summary>Si todavía no cargó nada, devuelve un perfil vacío sin persistir nada.</summary>
    Task<MiPerfilProfesorDto> ObtenerMioAsync(CancellationToken ct = default);
    Task<MiPerfilProfesorDto> GuardarMioAsync(GuardarPerfilProfesorDto dto, CancellationToken ct = default);

    Task<ImagenSubidaDto> SubirPortadaAsync(ImagenSubida imagen, CancellationToken ct = default);
    Task<ImagenSubidaDto> SubirAvatarAsync(ImagenSubida imagen, CancellationToken ct = default);
    Task QuitarPortadaAsync(CancellationToken ct = default);
    Task QuitarAvatarAsync(CancellationToken ct = default);

    Task<FotoPerfilDto> AgregarFotoAsync(ImagenSubida imagen, string? pieDeFoto, CancellationToken ct = default);
    Task CambiarPieDeFotoAsync(Guid fotoId, string? pieDeFoto, CancellationToken ct = default);
    Task EliminarFotoAsync(Guid fotoId, CancellationToken ct = default);
    Task ReordenarFotosAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);

    Task<HitoTrayectoriaDto> AgregarHitoAsync(GuardarHitoDto dto, CancellationToken ct = default);
    Task EditarHitoAsync(Guid hitoId, GuardarHitoDto dto, CancellationToken ct = default);
    Task EliminarHitoAsync(Guid hitoId, CancellationToken ct = default);
    Task ReordenarHitosAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);

    /// <summary>
    /// Borra el perfil de un usuario del tenant actual y sus archivos. Lo llama el
    /// borrado definitivo de un empleado, para no dejar sus fotos dando vueltas.
    /// </summary>
    Task EliminarPerfilDeUsuarioAsync(Guid userId, CancellationToken ct = default);

    // ── Lo que ve el alumno (club pedido por parámetro, no por token) ──

    Task<IReadOnlyList<ProfesorTarjetaDto>> ListarProfesoresDelClubAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>null si no existe, no está publicado o el profe ya no trabaja ahí (el controller lo traduce a 404).</summary>
    Task<PerfilProfesorPublicoDto?> ObtenerPublicoAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
}
