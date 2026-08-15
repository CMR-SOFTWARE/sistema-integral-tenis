using System.ComponentModel.DataAnnotations;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Una fila del leaderboard de DOBLES. JugadorId es el id de SINGLES (con eso se arma un desafío).</summary>
public class RankingFilaDoblesDto
{
    public Guid JugadorRankingDoblesId { get; set; }
    public Guid JugadorId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public int Posicion { get; set; }
    public int Puntos { get; set; }
    public string Rango { get; set; } = string.Empty;
    public int Cf { get; set; }
}

/// <summary>Mi perfil de ranking de dobles.</summary>
public class MiPerfilDoblesDto
{
    public bool Inscripto { get; set; }
    public Guid? JugadorRankingDoblesId { get; set; }
    public int? Posicion { get; set; }
    public int Puntos { get; set; }
    public string? Rango { get; set; }
    public int? Cf { get; set; }
    public int? MejorPuestoHistorico { get; set; }
}

/// <summary>Perfil público de dobles de CUALQUIER jugador (se ve al tocar su fila en la tabla).</summary>
public class PerfilPublicoRankingDoblesDto
{
    public Guid JugadorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public int? Posicion { get; set; }
    public int Puntos { get; set; }
    public string? Rango { get; set; }
    public int? Cf { get; set; }
    public int? MejorPuestoHistorico { get; set; }
}

/// <summary>Desafío de dobles: mi compañero + los dos rivales, todos por su JugadorId de singles.</summary>
public class ProponerDesafioDoblesDto
{
    [Required]
    public Guid CompaneroJugadorId { get; set; }

    [Required]
    public Guid Rival1JugadorId { get; set; }

    [Required]
    public Guid Rival2JugadorId { get; set; }
}

/// <summary>Quién ganó — cualquiera de los 4 jugadores del partido; el service resuelve la pareja.</summary>
public class FinalizarDesafioDoblesDto
{
    [Required]
    public Guid GanadorJugadorId { get; set; }
}

/// <summary>Un desafío de dobles (propuesto/aceptado/finalizado), con los nombres ya resueltos.</summary>
public class DesafioDoblesDto
{
    public Guid Id { get; set; }
    public Guid Jugador1Id { get; set; }
    public string Jugador1Nombre { get; set; } = string.Empty;
    public Guid Jugador2Id { get; set; }
    public string Jugador2Nombre { get; set; } = string.Empty;
    public Guid Rival1Id { get; set; }
    public string Rival1Nombre { get; set; } = string.Empty;
    public Guid Rival2Id { get; set; }
    public string Rival2Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime CreadoEl { get; set; }
    public DateTime? AceptadoEl { get; set; }
    public bool? GanoParejaA { get; set; }
    public int? PuntosGanadores { get; set; }
    public int? PuntosPerdedores { get; set; }
    public DateTime? FinalizadoEn { get; set; }
}
