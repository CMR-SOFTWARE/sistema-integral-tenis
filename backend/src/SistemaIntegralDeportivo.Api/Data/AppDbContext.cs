using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Data;

/// <summary>
/// El puente entre las entidades C# y la base SQLite. Cada DbSet es una tabla;
/// OnModelCreating ajusta lo que la convención por defecto no resuelve sola
/// (clave compuesta, índices únicos, enums como texto, datos semilla).
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Id fijo del tenant de demostración. Mientras no haya login real, todo
    /// el sistema opera sobre este tenant (ver plan: "tenant demo fijo").
    /// </summary>
    public static readonly Guid TenantDemoId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Alumno> Alumnos => Set<Alumno>();
    public DbSet<Tutor> Tutores => Set<Tutor>();
    public DbSet<Grupo> Grupos => Set<Grupo>();
    public DbSet<AlumnoGrupo> AlumnoGrupos => Set<AlumnoGrupo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Enums guardados como TEXTO en la base (legibles al inspeccionar
        //    el .db, en vez de 0,1,2...) ──
        modelBuilder.Entity<Tenant>().Property(t => t.Tipo).HasConversion<string>();
        modelBuilder.Entity<Alumno>().Property(a => a.Categoria).HasConversion<string>();
        modelBuilder.Entity<Alumno>().Property(a => a.Estado).HasConversion<string>();
        modelBuilder.Entity<Tutor>().Property(t => t.Relacion).HasConversion<string>();
        modelBuilder.Entity<Grupo>().Property(g => g.Categoria).HasConversion<string>();

        // ── Tenant: subdominio único ──
        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Subdominio)
            .IsUnique();

        // ── Alumno: DNI único POR tenant (no global). La misma persona puede
        //    ser alumna de dos profes distintos → dos registros. ──
        modelBuilder.Entity<Alumno>()
            .HasIndex(a => new { a.TenantId, a.Dni })
            .IsUnique();

        // ── Alumno: índice para el query más frecuente (activos de un tenant) ──
        modelBuilder.Entity<Alumno>()
            .HasIndex(a => new { a.TenantId, a.Estado });

        // ── Alumno → Tutor: si se borra el tutor, el alumno NO se borra,
        //    solo queda sin tutor (FK nullable → SetNull) ──
        modelBuilder.Entity<Alumno>()
            .HasOne(a => a.Tutor)
            .WithMany(t => t.Alumnos)
            .HasForeignKey(a => a.TutorId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Arancel: precisión monetaria (para cuando migremos a SQL Server/Postgres) ──
        modelBuilder.Entity<Alumno>()
            .Property(a => a.Arancel)
            .HasPrecision(12, 2);

        // ── Tutor: DNI único por tenant ──
        modelBuilder.Entity<Tutor>()
            .HasIndex(t => new { t.TenantId, t.Dni })
            .IsUnique();

        // ── Grupo: índice por tenant + activo ──
        modelBuilder.Entity<Grupo>()
            .HasIndex(g => new { g.TenantId, g.Activo });

        // ── AlumnoGrupo: clave primaria COMPUESTA (alumno + grupo) ──
        modelBuilder.Entity<AlumnoGrupo>()
            .HasKey(ag => new { ag.AlumnoId, ag.GrupoId });

        // ── Datos semilla: el tenant demo (valores fijos, sin Guid.NewGuid()
        //    ni DateTime.Now, porque HasData exige datos determinísticos) ──
        modelBuilder.Entity<Tenant>().HasData(new Tenant
        {
            Id = TenantDemoId,
            Subdominio = "demo",
            Nombre = "Club Demo",
            Tipo = TipoTenant.Profesor,
            Activo = true,
            CreadoEl = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
