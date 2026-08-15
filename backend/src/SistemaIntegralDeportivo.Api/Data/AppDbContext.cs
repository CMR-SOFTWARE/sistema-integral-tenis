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
    public DbSet<PedidoLinea> PedidoLineas => Set<PedidoLinea>();
    public DbSet<Raqueta> Raquetas => Set<Raqueta>();
    public DbSet<Encordado> Encordados => Set<Encordado>();
    public DbSet<SolicitudHorario> SolicitudesHorario => Set<SolicitudHorario>();
    public DbSet<ClaseSuelta> ClasesSueltas => Set<ClaseSuelta>();
    public DbSet<Publicidad> Publicidades => Set<Publicidad>();
    public DbSet<Noticia> Noticias => Set<Noticia>();
    public DbSet<NotaAlumno> NotasAlumno => Set<NotaAlumno>();
    public DbSet<MembresiaTenant> MembresiasTenant => Set<MembresiaTenant>();
    public DbSet<PagoEmpleado> PagosEmpleado => Set<PagoEmpleado>();
    public DbSet<AlumnoHorario> AlumnoHorarios => Set<AlumnoHorario>();
    public DbSet<SolicitudCupo> SolicitudesCupo => Set<SolicitudCupo>();
    public DbSet<PerfilProfesor> PerfilesProfesor => Set<PerfilProfesor>();
    public DbSet<FotoPerfil> FotosPerfil => Set<FotoPerfil>();
    public DbSet<HitoTrayectoria> HitosTrayectoria => Set<HitoTrayectoria>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<JugadorRanking> JugadoresRanking => Set<JugadorRanking>();
    public DbSet<JuegoPendiente> JuegosPendientes => Set<JuegoPendiente>();
    public DbSet<PuntosMovimiento> PuntosMovimientos => Set<PuntosMovimiento>();
    public DbSet<JugadorRankingDobles> JugadoresRankingDobles => Set<JugadorRankingDobles>();
    public DbSet<JuegoDoblesPendiente> JuegosDoblesPendientes => Set<JuegoDoblesPendiente>();
    public DbSet<PuntosMovimientoDobles> PuntosMovimientosDobles => Set<PuntosMovimientoDobles>();
    public DbSet<RankingSnapshot> RankingSnapshots => Set<RankingSnapshot>();
    public DbSet<JuegoRevision> JuegosRevision => Set<JuegoRevision>();

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

        // FechaNacimiento es una FECHA, no un instante: el front manda "2000-01-01"
        // sin zona horaria (DateTime.Kind=Unspecified). Npgsql por default mapea
        // DateTime a "timestamp with time zone" y EXIGE Kind=Utc, así que sin esto
        // el alta de alumno explota con un 500 en Postgres (no pasaba en SQLite).
        modelBuilder.Entity<Alumno>().Property(a => a.FechaNacimiento).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<Usuario>().Property(u => u.FechaNacimiento).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<MembresiaTenant>().Property(m => m.Rol).HasConversion<string>();

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

        // ── Agenda: sedes, canchas, horarios y turnos ──

        modelBuilder.Entity<Sede>()
            .HasIndex(s => new { s.TenantId, s.Nombre })
            .IsUnique(); // sin dos sedes con el mismo nombre en el tenant

        modelBuilder.Entity<Cancha>()
            .HasIndex(c => new { c.SedeId, c.Nombre })
            .IsUnique();

        modelBuilder.Entity<Horario>().Property(h => h.Dia).HasConversion<string>();
        modelBuilder.Entity<Horario>().Property(h => h.Categoria).HasConversion<string>();

        // ── El roster del horario: quiénes toman esa clase ──
        // Misma forma que la vieja AlumnoGrupo: PK compuesta para que el reingreso
        // reactive la fila (y no duplique), con FechaAlta/FechaBaja como historia.
        modelBuilder.Entity<AlumnoHorario>()
            .HasKey(ah => new { ah.AlumnoId, ah.HorarioId });

        modelBuilder.Entity<AlumnoHorario>()
            .HasOne(ah => ah.Horario).WithMany(h => h.Alumnos).HasForeignKey(ah => ah.HorarioId)
            .OnDelete(DeleteBehavior.Cascade); // se borra el horario → se va su roster

        modelBuilder.Entity<AlumnoHorario>()
            .HasIndex(ah => ah.HorarioId); // "quiénes vienen a esta clase"

        // ── Pedidos de un lugar en una clase (portal del alumno) ──
        modelBuilder.Entity<SolicitudCupo>().Property(s => s.Estado).HasConversion<string>();
        modelBuilder.Entity<SolicitudCupo>()
            .HasIndex(s => new { s.TenantId, s.Estado }); // "las pendientes del profe"
        modelBuilder.Entity<SolicitudCupo>()
            .HasOne(s => s.Alumno).WithMany().HasForeignKey(s => s.AlumnoId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SolicitudCupo>()
            .HasOne(s => s.Horario).WithMany().HasForeignKey(s => s.HorarioId)
            .OnDelete(DeleteBehavior.Cascade);
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

        // MembresiaTenant.SedeId (el club del empleado): borrar la sede no borra la membresía
        modelBuilder.Entity<MembresiaTenant>()
            .HasOne(m => m.Sede)
            .WithMany()
            .HasForeignKey(m => m.SedeId)
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

        // Default en la BASE (no solo en C#): así los tenants YA existentes quedan
        // en true al aplicar la migración (el dueño sigue siendo profe asignable).
        modelBuilder.Entity<Tenant>().Property(t => t.DirectorDaClases).HasDefaultValue(true);

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
        modelBuilder.Entity<Pedido>()
            .HasIndex(p => new { p.TenantId, p.Estado }); // "pedidos pendientes del profe"

        // El cargo que nació del pedido: si se borrara, el pedido queda sin él
        modelBuilder.Entity<Pedido>()
            .HasOne(p => p.Cargo)
            .WithMany()
            .HasForeignKey(p => p.CargoId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PedidoLinea>().Property(l => l.PrecioUnitario).HasPrecision(12, 2);

        // Una línea no existe sin su pedido
        modelBuilder.Entity<PedidoLinea>()
            .HasOne(l => l.Pedido)
            .WithMany(p => p.Lineas)
            .HasForeignKey(l => l.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        // El servicio puede desactivarse pero la línea conserva su snapshot:
        // si se borrara el servicio, el pedido histórico no se rompe
        modelBuilder.Entity<PedidoLinea>()
            .HasOne(l => l.Servicio)
            .WithMany()
            .HasForeignKey(l => l.ServicioId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Raquetas del alumno (M3) ──

        modelBuilder.Entity<Raqueta>()
            .HasOne(r => r.Alumno)
            .WithMany(a => a.Raquetas)
            .HasForeignKey(r => r.AlumnoId)
            .OnDelete(DeleteBehavior.Cascade); // si se borrara el alumno, se van sus raquetas
        modelBuilder.Entity<Raqueta>()
            .HasIndex(r => r.AlumnoId); // "las raquetas de este alumno"

        modelBuilder.Entity<Encordado>()
            .HasOne(e => e.Raqueta)
            .WithMany(r => r.Encordados)
            .HasForeignKey(e => e.RaquetaId)
            .OnDelete(DeleteBehavior.Cascade); // se borra la raqueta → se va su historial
        // "el historial de esta raqueta, del más nuevo al más viejo"
        modelBuilder.Entity<Encordado>()
            .HasIndex(e => new { e.RaquetaId, e.Fecha });

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

        // ── Publicidad (M6): banners por tenant ──

        modelBuilder.Entity<Publicidad>()
            .HasIndex(p => p.TenantId); // "los banners de este club"

        // ── Noticias: el tablón del club, por tenant ──

        modelBuilder.Entity<Noticia>()
            .HasIndex(n => n.TenantId); // "las noticias de este club"

        // ── Membresías Staff: el profe empleado dentro del tenant del head pro ──

        modelBuilder.Entity<MembresiaTenant>()
            .HasIndex(m => m.TenantId); // "los profes de este club"

        // Una persona no se agrega dos veces al mismo club.
        modelBuilder.Entity<MembresiaTenant>()
            .HasIndex(m => new { m.TenantId, m.UserId })
            .IsUnique();

        // Resolver rápido "¿de qué clubes es staff este usuario?" al armar la sesión.
        modelBuilder.Entity<MembresiaTenant>()
            .HasIndex(m => m.UserId);

        // Valor hora base del empleado: precisión monetaria.
        modelBuilder.Entity<MembresiaTenant>()
            .Property(m => m.ValorHora)
            .HasPrecision(12, 2);

        // ── Sueldos: valor hora por horario + pagos a empleados (G3) ──

        modelBuilder.Entity<Horario>()
            .Property(h => h.ValorHoraProfe)
            .HasPrecision(12, 2);

        modelBuilder.Entity<PagoEmpleado>().Property(p => p.MedioPago).HasConversion<string>();
        modelBuilder.Entity<PagoEmpleado>().Property(p => p.Monto).HasPrecision(12, 2);

        // Un sueldo por (empleado, mes): idempotencia del pago mensual.
        modelBuilder.Entity<PagoEmpleado>()
            .HasIndex(p => new { p.TenantId, p.UserId, p.Anio, p.Mes })
            .IsUnique();

        // ── Notas por alumno: seguimiento privado/compartido del profe ──

        modelBuilder.Entity<NotaAlumno>()
            .HasIndex(n => n.AlumnoId); // "las notas de este alumno"

        // Si se borrara el alumno, se van sus notas (hoy la baja es lógica).
        modelBuilder.Entity<NotaAlumno>()
            .HasOne(n => n.Alumno).WithMany().HasForeignKey(n => n.AlumnoId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Perfil público del profe: su carta de presentación en el club ──

        // Un perfil por persona y por club. Sirve igual para el dueño (que no tiene
        // membresía) y para el staff, porque la llave es el par tenant+usuario.
        modelBuilder.Entity<PerfilProfesor>()
            .HasIndex(p => new { p.TenantId, p.UserId })
            .IsUnique();

        // El listado que ve el alumno: los perfiles publicados de su club
        modelBuilder.Entity<PerfilProfesor>()
            .HasIndex(p => new { p.TenantId, p.Publicado });

        // Si se borra el perfil se van sus fotos e hitos (las filas; los archivos
        // del storage los borra el service, que es el único que sabe de rutas)
        modelBuilder.Entity<FotoPerfil>()
            .HasOne(f => f.Perfil).WithMany(p => p.Fotos).HasForeignKey(f => f.PerfilProfesorId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<HitoTrayectoria>()
            .HasOne(h => h.Perfil).WithMany(p => p.Hitos).HasForeignKey(h => h.PerfilProfesorId)
            .OnDelete(DeleteBehavior.Cascade);

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
            .HasFilter("\"Estado\" = 'Pendiente'"); // Postgres es case-sensitive: la columna necesita comillas

        // El listado del profe: pendientes de SU club
        modelBuilder.Entity<Solicitud>()
            .HasIndex(s => new { s.TenantId, s.Estado });

        // ── Ranking R.U.T.A. (cross-tenant — ver JugadorRankingRepository) ──

        modelBuilder.Entity<Notificacion>()
            .HasIndex(n => new { n.DestinatarioUserId, n.Leida }); // "mis notificaciones sin leer"

        modelBuilder.Entity<JugadorRanking>()
            .HasIndex(j => j.UsuarioId)
            .IsUnique(); // 1:1 con Usuario

        modelBuilder.Entity<JugadorRanking>()
            .HasIndex(j => j.PuntosProvisionales); // ordenar el leaderboard

        // OrdenInscripcion es el desempate (nunca alfabético/random) y necesita su
        // propia secuencia: es un int aparte del Id (Guid), no la PK.
        modelBuilder.HasSequence<int>("OrdenInscripcionRanking").StartsAt(1);
        modelBuilder.Entity<JugadorRanking>()
            .Property(j => j.OrdenInscripcion)
            .HasDefaultValueSql("nextval('\"OrdenInscripcionRanking\"')");

        modelBuilder.Entity<JuegoPendiente>().Property(j => j.Estado).HasConversion<string>();

        // Un par de jugadores solo puede enfrentarse UNA VEZ en este flujo — el
        // contrato es explícito: bloquea incluso si el desafío ya está Finalizado
        // (nunca se borra), por eso el índice es único SIN filtro por estado.
        modelBuilder.Entity<JuegoPendiente>()
            .HasIndex(j => new { j.JugadorMenorId, j.JugadorMayorId })
            .IsUnique();

        modelBuilder.Entity<PuntosMovimiento>()
            .HasIndex(m => new { m.JugadorRankingId, m.Fecha }); // "puntos vigentes de un jugador"

        // ── Ranking de DOBLES (Fase 3) — espejo de singles, pool de puntos independiente ──

        modelBuilder.Entity<JugadorRankingDobles>()
            .HasIndex(j => j.JugadorRankingId)
            .IsUnique(); // 1:1 con JugadorRanking (no con Usuario directo)

        modelBuilder.Entity<JugadorRankingDobles>()
            .HasIndex(j => j.PuntosProvisionales);

        // Desempate propio de dobles: secuencia aparte de la de singles.
        modelBuilder.HasSequence<int>("OrdenInscripcionRankingDobles").StartsAt(1);
        modelBuilder.Entity<JugadorRankingDobles>()
            .Property(j => j.OrdenInscripcion)
            .HasDefaultValueSql("nextval('\"OrdenInscripcionRankingDobles\"')");

        modelBuilder.Entity<JuegoDoblesPendiente>().Property(j => j.Estado).HasConversion<string>();

        // Sin índice único acá a propósito: el bloqueo pareja-vs-pareja (revancha
        // permitida tras Finalizado) se valida en el Service, no en la base.

        modelBuilder.Entity<PuntosMovimientoDobles>()
            .HasIndex(m => new { m.JugadorRankingDoblesId, m.Fecha });

        // ── Cierre oficial + revisiones (Fase 4) ──

        modelBuilder.Entity<RankingSnapshot>().Property(s => s.Modalidad).HasConversion<string>();
        modelBuilder.Entity<RankingSnapshot>().Property(s => s.Scope).HasConversion<string>();

        // "el snapshot oficial vigente de tal modalidad+scope+valor" y "¿ya cerró hoy?"
        modelBuilder.Entity<RankingSnapshot>()
            .HasIndex(s => new { s.Modalidad, s.Scope, s.ScopeValor, s.FechaCorte });

        modelBuilder.Entity<JuegoRevision>().Property(r => r.Estado).HasConversion<string>();

        modelBuilder.Entity<JuegoRevision>()
            .HasIndex(r => new { r.JuegoPendienteId, r.JuegoDoblesPendienteId, r.Estado });

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
