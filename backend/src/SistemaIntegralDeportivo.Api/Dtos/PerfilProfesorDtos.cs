using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

// ── Lo que edita el profe (su propio perfil) ──

public class MiPerfilProfesorDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Club { get; set; } = string.Empty;
    public string? Titular { get; set; }
    public string? Subtitulo { get; set; }
    public string? Bio { get; set; }
    public List<string> Especialidades { get; set; } = [];
    public string? PortadaUrl { get; set; }
    public string? AvatarUrl { get; set; }
    public bool Publicado { get; set; }
    public List<FotoPerfilDto> Fotos { get; set; } = [];
    public List<HitoTrayectoriaDto> Hitos { get; set; } = [];
}

public class GuardarPerfilProfesorDto
{
    [StringLength(80)]
    public string? Titular { get; set; }

    [StringLength(120)]
    public string? Subtitulo { get; set; }

    [StringLength(2000)]
    public string? Bio { get; set; }

    public List<string> Especialidades { get; set; } = [];

    public bool Publicado { get; set; }
}

public class FotoPerfilDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? PieDeFoto { get; set; }
    public int Orden { get; set; }
}

public class GuardarPieDeFotoDto
{
    [StringLength(120)]
    public string? PieDeFoto { get; set; }
}

public class HitoTrayectoriaDto
{
    public Guid Id { get; set; }
    public int Anio { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public int Orden { get; set; }
}

public class GuardarHitoDto
{
    public int Anio { get; set; }

    [Required, StringLength(120)]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(400)]
    public string? Detalle { get; set; }
}

/// <summary>El orden nuevo, completo: la posición de cada id es su Orden.</summary>
public class ReordenarDto
{
    public List<Guid> Ids { get; set; } = [];
}

public class ImagenSubidaDto
{
    public string Url { get; set; } = string.Empty;
}

// ── Lo que ven los alumnos (y quien está buscando club) ──

/// <summary>La tarjeta de un profe en la lista del club.</summary>
public class ProfesorTarjetaDto
{
    public Guid UserId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public bool EsDueño { get; set; }
    public string? Titular { get; set; }
    public string? AvatarUrl { get; set; }
    public List<string> Especialidades { get; set; } = [];
    /// <summary>Si es false, la tarjeta se muestra igual pero sin el link "Ver perfil".</summary>
    public bool TienePerfil { get; set; }
}

public class PerfilProfesorPublicoDto
{
    public Guid UserId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Club { get; set; } = string.Empty;
    public string? Titular { get; set; }
    public string? Subtitulo { get; set; }
    public string? Bio { get; set; }
    public List<string> Especialidades { get; set; } = [];
    public string? PortadaUrl { get; set; }
    public string? AvatarUrl { get; set; }
    public List<FotoPerfilDto> Fotos { get; set; } = [];
    public List<HitoTrayectoriaDto> Hitos { get; set; } = [];
}
