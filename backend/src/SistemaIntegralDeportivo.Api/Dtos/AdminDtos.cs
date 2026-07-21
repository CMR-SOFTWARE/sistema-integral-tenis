using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Dtos;

/// <summary>Métricas globales de la plataforma (todos los clubes juntos).</summary>
public class MetricasPlataformaDto
{
    public int TotalClubes { get; set; }
    public int ClubesActivos { get; set; }
    public int ClubesPendientes { get; set; }
    public int ClubesSuspendidos { get; set; }

    /// <summary>Dueños + staff activos de toda la plataforma.</summary>
    public int TotalProfes { get; set; }
    public int TotalAlumnos { get; set; }

    /// <summary>Suma de pagos confirmados en el mes en curso (aprox. de facturación).</summary>
    public decimal IngresosMes { get; set; }

    public int ClubesNuevos30d { get; set; }
    public int AlumnosNuevos30d { get; set; }
}

/// <summary>Un club/tenant en la lista del admin (con su profe dueño y su tamaño).</summary>
public class ClubAdminDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Subdominio { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Profesor { get; set; } = string.Empty;
    public int Alumnos { get; set; }
    public DateTime CreadoEl { get; set; }
}

/// <summary>Body para activar/suspender un club (solo Activo o Suspendido).</summary>
public class CambiarEstadoClubDto
{
    public EstadoTenant Estado { get; set; }
}
