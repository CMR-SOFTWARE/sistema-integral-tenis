namespace SistemaIntegralDeportivo.Api.Models;

/// <summary>
/// Inscripción de un Usuario (identidad GLOBAL) al ranking R.U.T.A. — cross-tenant,
/// independiente de en qué club(es) juegue. 1:1 con Usuario, se crea on-demand
/// cuando alguien se inscribe (no hay seed masivo de todos los AspNetUsers).
/// </summary>
public class JugadorRanking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UsuarioId { get; set; }

    // ── Perfil (datos opcionales donde no siempre se tienen — principio del producto) ──
    public string? Sexo { get; set; }
    public string? CiudadResidencia { get; set; }
    public string? Provincia { get; set; }
    public string? Pais { get; set; }
    public string? Licencia { get; set; }
    public string? Mano { get; set; }
    public string? Reves { get; set; }
    public string? Bio { get; set; }
    public bool PerfilPublico { get; set; } = true;
    public bool PermiteContacto { get; set; } = true;

    // ── Stats provisionales (se recalculan enteras tras cada partido, nunca se suman a mano) ──
    public int PuntosProvisionales { get; set; }
    public int? PosicionProvisional { get; set; }
    public string? RangoProvisional { get; set; }
    public int? CfProvisional { get; set; }

    /// <summary>Desempate: quien se inscribió antes queda arriba. Nunca alfabético ni random. Secuencia propia, no el Id.</summary>
    public int OrdenInscripcion { get; set; }

    public int? MejorPuestoHistorico { get; set; }
    public DateTime? FechaMejorPuesto { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime InscriptoEl { get; set; } = DateTime.UtcNow;
}
