using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

public class CreateSedeDto
{
    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;
}

public class CreateCanchaDto
{
    [Required, StringLength(40)]
    public string Nombre { get; set; } = string.Empty;
}

public class CanchaResponseDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public class SedeResponseDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public List<CanchaResponseDto> Canchas { get; set; } = [];
}
