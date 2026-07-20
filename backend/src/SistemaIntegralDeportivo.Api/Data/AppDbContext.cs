using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Data;

/// <summary>
/// El puente entre las entidades C# y la base SQLite. Cada DbSet es una tabla;
/// OnModelCreating ajusta lo que la convención por defecto no resuelve sola
/// (clave compuesta, índices únicos, enums como texto, datos semilla).
/// Hereda de IdentityUserContext: suma las tablas de Identity para Usuario
/// (sin las de roles: los roles son membresías por tenant, ADR-0007).
/// </summary>
public class AppDbContext : IdentityUserContext<Usuario, Guid>
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
    public DbSet<Sede> Sedes => Set<Sede>();
    public DbSet<Cancha> Canchas => Set<Cancha>();
    public DbSet<Horario> Horarios => Set<Horario>();
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<TurnoParticipante> TurnoParticipantes => Set<TurnoParticipante>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<Bloqueo> Bloqueos => Set<Bloqueo>();
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<Raqueta> Raquetas => Set<Raqueta>();
    public DbSet<SolicitudGrupo> SolicitudesGrupo => Set<SolicitudGrupo>();
    public DbSet<SolicitudHorario> SolicitudesHorario => Set<SolicitudHorario>();
    public DbSet<ClaseSuelta> ClasesSueltas => Set<ClaseSuelta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Enums guardados como TEXTO en la base (legibles al inspeccionar
        //    el .db, en vez de 0,1,2...) ──
        modelBuilder.Entity<Tenant>().Property(t => t.Tipo).HasConversion<string>();
        modelBuilder.Entity<Tenant>().Property(t => t.Estado).HasConversion<string>();
        modelBuilder.Entity<Usuario>().Property(u => u.Categoria).HasConversion<string>();
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

        // ── Agenda: sedes, canchas, horarios y turnos ──

        modelBuilder.Entity<Sede>()
            .HasIndex(s => new { s.TenantId, s.Nombre })
            .IsUnique(); // sin dos sedes con el mismo nombre en el tenant

        modelBuilder.Entity<Cancha>()
            .HasIndex(c => new { c.SedeId, c.Nombre })
            .IsUnique();

        modelBuilder.Entity<Horario>().Property(h => h.Dia).HasConversion<string>();
        modelBuilder.Entity<Horario>()
            .HasIndex(h => new { h.TenantId, h.Activo }); // "horarios activos del profe"
        modelBuilder.Entity<Horario>()
            .HasIndex(h => new { h.CanchaId, h.Dia });    // chequeo de solapamiento

        // Alumno.SedeId es informativo: borrar la sede no borra alumnos
        modelBuilder.Entity<Alumno>()
            .HasOne(a => a.Sede)
            .WithMany()
            .HasForeignKey(a => a.SedeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Turno>().Property(t => t.Estado).HasConversion<string>();
        modelBuilder.Entity<Turno>().Property(t => t.CanceladoPor).HasConversion<string>();
        // Idempotencia de la generación: UN turno por horario y fecha
        modelBuilder.Entity<Turno>()
            .HasIndex(t => new { t.HorarioId, t.Fecha })
            .IsUnique();
        modelBuilder.Entity<Turno>()
            .HasIndex(t => new { t.TenantId, t.Fecha }); // "turnos de la semana"

        // Roster: PK compuesta (un alumno una vez por turno)
        modelBuilder.Entity<TurnoParticipante>()
            .HasKey(tp => new { tp.TurnoId, tp.AlumnoId });

        // ── Cuenta corriente: cargos (ADR-0009) ──

        modelBuilder.Entity<Alumno>().Property(a => a.Modalidad).HasConversion<string>();

        modelBuilder.Entity<Tenant>().Property(t => t.ValorHoraGrupal).HasPrecision(12, 2);
        modelBuilder.Entity<Tenant>().Property(t => t.ValorClaseIndividual).HasPrecision(12, 2);

        modelBuilder.Entity<Cargo>().Property(c => c.Tipo).HasConversion<string>();
        modelBuilder.Entity<Cargo>().Property(c => c.MedioPago).HasConversion<string>();
        modelBuilder.Entity<Cargo>().Property(c => c.Monto).HasPrecision(12, 2);

        // Idempotencia del cargo de clase: UNO por (turno, alumno).
        // Los cargos manuales tienen TurnoId null (los NULL no chocan entre sí).
        modelBuilder.Entity<Cargo>()
            .HasIndex(c => new { c.TurnoId, c.AlumnoId })
            .IsUnique();

        // El query de la liquidación: cargos de un alumno en un período
        modelBuilder.Entity<Cargo>()
            .HasIndex(c => new { c.TenantId, c.AlumnoId, c.Fecha });

        // Si se borrara un turno, el cargo (plata, historia) NO se borra
        modelBuilder.Entity<Cargo>()
            .HasOne(c => c.Turno)
            .WithMany()
            .HasForeignKey(c => c.TurnoId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Servicios (catálogo del profe) y Pedidos del alumno (M4) ──

        modelBuilder.Entity<Servicio>().Property(s => s.Precio).HasPrecision(12, 2);
        modelBuilder.Entity<Servicio>()
            .HasIndex(s => s.TenantId); // "el catálogo del profe"

        modelBuilder.Entity<Pedido>().Property(p => p.Estado).HasConversion<string>();
        modelBuilder.Entity<Pedido>().Property(p => p.Precio).HasPrecision(12, 2);
        modelBuilder.Entity<Pedido>()
            .HasIndex(p => new { p.TenantId, p.Estado }); // "pedidos pendientes del profe"

        // El servicio puede desactivarse pero el pedido conserva su snapshot:
        // si se borrara el servicio, el pedido histórico no se rompe
        modelBuilder.Entity<Pedido>()
            .HasOne(p => p.Servicio)
            .WithMany()
            .HasForeignKey(p => p.ServicioId)
            .OnDelete(DeleteBehavior.Restrict);

        // El cargo que nació del pedido: si se borrara, el pedido queda sin él
        modelBuilder.Entity<Pedido>()
            .HasOne(p => p.Cargo)
            .WithMany()
            .HasForeignKey(p => p.CargoId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Raquetas del alumno (M3) ──

        modelBuilder.Entity<Raqueta>()
            .HasOne(r => r.Alumno)
            .WithMany(a => a.Raquetas)
            .HasForeignKey(r => r.AlumnoId)
            .OnDelete(DeleteBehavior.Cascade); // si se borrara el alumno, se van sus raquetas
        modelBuilder.Entity<Raqueta>()
            .HasIndex(r => r.AlumnoId); // "las raquetas de este alumno"

        // ── Solicitudes de sumarse a un grupo (M5a) ──

        modelBuilder.Entity<SolicitudGrupo>().Property(s => s.Estado).HasConversion<string>();
        modelBuilder.Entity<SolicitudGrupo>()
            .HasIndex(s => new { s.TenantId, s.Estado }); // "solicitudes pendientes del profe"
        modelBuilder.Entity<SolicitudGrupo>()
            .HasOne(s => s.Alumno).WithMany().HasForeignKey(s => s.AlumnoId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SolicitudGrupo>()
            .HasOne(s => s.Grupo).WithMany().HasForeignKey(s => s.GrupoId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Solicitudes de clase individual fija (M5b) ──

        modelBuilder.Entity<SolicitudHorario>().Property(s => s.Estado).HasConversion<string>();
        modelBuilder.Entity<SolicitudHorario>().Property(s => s.Dia).HasConversion<string>();
        modelBuilder.Entity<SolicitudHorario>()
            .HasIndex(s => new { s.TenantId, s.Estado }); // "solicitudes individuales pendientes del profe"
        modelBuilder.Entity<SolicitudHorario>()
            .HasOne(s => s.Alumno).WithMany().HasForeignKey(s => s.AlumnoId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SolicitudHorario>()
            .HasOne(s => s.Sede).WithMany().HasForeignKey(s => s.SedeId)
            .OnDelete(DeleteBehavior.Restrict);
        // La cancha y el horario se completan al aceptar; si se borraran, la
        // solicitud (historia) no se rompe
        modelBuilder.Entity<SolicitudHorario>()
            .HasOne(s => s.Cancha).WithMany().HasForeignKey(s => s.CanchaId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<SolicitudHorario>()
            .HasOne(s => s.Horario).WithMany().HasForeignKey(s => s.HorarioId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Clases sueltas (M5c) ──

        modelBuilder.Entity<ClaseSuelta>().Property(c => c.Estado).HasConversion<string>();
        modelBuilder.Entity<ClaseSuelta>()
            .HasIndex(c => new { c.TenantId, c.Estado }); // "clases sueltas pendientes del profe"
        modelBuilder.Entity<ClaseSuelta>()
            .HasOne(c => c.Alumno).WithMany().HasForeignKey(c => c.AlumnoId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ClaseSuelta>()
            .HasOne(c => c.Sede).WithMany().HasForeignKey(c => c.SedeId)
            .OnDelete(DeleteBehavior.Restrict);
        // El cargo (plata) es la ancla del pago; al rechazar se borra el cargo y
        // la clase queda como historia con CargoId en null
        modelBuilder.Entity<ClaseSuelta>()
            .HasOne(c => c.Cargo).WithMany().HasForeignKey(c => c.CargoId)
            .OnDelete(DeleteBehavior.SetNull);
        // Cancha y turno se completan al confirmar; si se borraran, no rompen la historia
        modelBuilder.Entity<ClaseSuelta>()
            .HasOne(c => c.Cancha).WithMany().HasForeignKey(c => c.CanchaId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<ClaseSuelta>()
            .HasOne(c => c.Turno).WithMany().HasForeignKey(c => c.TurnoId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Bloqueos de agenda ──

        modelBuilder.Entity<Bloqueo>().Property(b => b.Tipo).HasConversion<string>();
        modelBuilder.Entity<Bloqueo>().Property(b => b.Dia).HasConversion<string>();
        modelBuilder.Entity<Bloqueo>().Property(b => b.Motivo).HasConversion<string>();

        modelBuilder.Entity<Bloqueo>()
            .HasIndex(b => b.TenantId); // "bloqueos del profe" (lista y salteo)

        // Si se borra la cancha, el bloqueo pasa a "todas" en vez de romperse
        modelBuilder.Entity<Bloqueo>()
            .HasOne(b => b.Cancha)
            .WithMany()
            .HasForeignKey(b => b.CanchaId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Solicitudes alumno→profe (plan v2, reemplaza al reclamo) ──

        modelBuilder.Entity<Solicitud>().Property(s => s.Estado).HasConversion<string>();

        // UNA sola pendiente por (usuario, club) — índice único PARCIAL
        modelBuilder.Entity<Solicitud>()
            .HasIndex(s => new { s.UserId, s.TenantId })
            .IsUnique()
            .HasFilter("Estado = 'Pendiente'");

        // El listado del profe: pendientes de SU club
        modelBuilder.Entity<Solicitud>()
            .HasIndex(s => new { s.TenantId, s.Estado });

        // ── Datos semilla: el tenant demo (valores fijos, sin Guid.NewGuid()
        //    ni DateTime.Now, porque HasData exige datos determinísticos) ──
        modelBuilder.Entity<Tenant>().HasData(new Tenant
        {
            Id = TenantDemoId,
            Subdominio = "demo",
            Nombre = "Club Demo",
            Tipo = TipoTenant.Profesor,
            Estado = EstadoTenant.Activo, // el demo no pasa por el checkout
            CreadoEl = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
