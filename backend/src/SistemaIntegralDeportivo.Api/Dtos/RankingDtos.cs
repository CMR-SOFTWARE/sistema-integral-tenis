using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Una fila del leaderboard (ranking provisional, en vivo).</summary>
public class RankingFilaDto
{
    public Guid JugadorId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public int Posicion { get; set; }
    public int Puntos { get; set; }
    public string Rango { get; set; } = string.Empty;
    public int Cf { get; set; }
}

/// <summary>Alta al ranking (datos opcionales — puede llegar vacío y completarse después).</summary>
public class InscribirmeRankingDto
{
    public string? Sexo { get; set; }
    public string? CiudadResidencia { get; set; }
    public string? Provincia { get; set; }
    public string? Pais { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }
}

/// <summary>Mi perfil de ranking: inscripción + posición actual (si ya jugó algo).</summary>
public class MiPerfilRankingDto
{
    public bool Inscripto { get; set; }
    public Guid? JugadorId { get; set; }
    public int? Posicion { get; set; }
    public int Puntos { get; set; }
    public string? Rango { get; set; }
    public int? Cf { get; set; }
    public string? CiudadResidencia { get; set; }
    public string? Provincia { get; set; }
    public string? Pais { get; set; }
    public string? Bio { get; set; }
    public int? MejorPuestoHistorico { get; set; }
}

/// <summary>Perfil público de CUALQUIER jugador del ranking (se ve al tocar su fila en la tabla).</summary>
public class PerfilPublicoRankingDto
{
    public Guid JugadorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public int? Posicion { get; set; }
    public int Puntos { get; set; }
    public string? Rango { get; set; }
    public int? Cf { get; set; }
    public string? CiudadResidencia { get; set; }
    public string? Provincia { get; set; }
    public string? Bio { get; set; }
    public int? MejorPuestoHistorico { get; set; }
}

/// <summary>Desafío a otro jugador: solo el id del rival (jugadorId, no usuarioId).</summary>
public class ProponerDesafioDto
{
    [Required]
    public Guid RivalJugadorId { get; set; }
}

/// <summary>Quién ganó — nada de resultado en texto (no "6-4 6-4").</summary>
public class FinalizarDesafioDto
{
    [Required]
    public Guid GanadorJugadorId { get; set; }
}

/// <summary>Un desafío (propuesto/aceptado/finalizado), con los nombres ya resueltos.</summary>
public class DesafioDto
{
    public Guid Id { get; set; }
    public Guid Jugador1Id { get; set; }
    public string Jugador1Nombre { get; set; } = string.Empty;
    public Guid Jugador2Id { get; set; }
    public string Jugador2Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime CreadoEl { get; set; }
    public DateTime? AceptadoEl { get; set; }
    public Guid? GanadorId { get; set; }
    public int? PuntosGanador { get; set; }
    public int? PuntosPerdedor { get; set; }
    public DateTime? FinalizadoEn { get; set; }
}
