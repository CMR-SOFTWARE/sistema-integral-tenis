using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaIntegralDeportivo.Api.Data;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Enums como texto en el JSON ("Cuarta", "Activo") en vez de números,
// coherente con cómo se guardan en la base y legible para el front.
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// El tenant del request (ADR-0010): claim "tenant" del JWT u override del portal
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantActual, TenantActual>();

// DI del módulo Alumnos: las capas se consumen por interfaz (ADR-0002)
builder.Services.AddScoped<IAlumnoRepository, AlumnoRepository>();
builder.Services.AddScoped<IAlumnoService, AlumnoService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IGrupoRepository, GrupoRepository>();
builder.Services.AddScoped<IGrupoService, GrupoService>();
builder.Services.AddScoped<ISedeRepository, SedeRepository>();
builder.Services.AddScoped<ISedeService, SedeService>();
builder.Services.AddScoped<IHorarioRepository, HorarioRepository>();
builder.Services.AddScoped<IHorarioService, HorarioService>();
builder.Services.AddScoped<ITurnoRepository, TurnoRepository>();
builder.Services.AddScoped<ITurnoService, TurnoService>();
builder.Services.AddScoped<ICargoRepository, CargoRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ICuotaService, CuotaService>();
builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPortalService, PortalService>();
builder.Services.AddScoped<IBloqueoRepository, BloqueoRepository>();
builder.Services.AddScoped<IBloqueoService, BloqueoService>();
builder.Services.AddScoped<ICancelacionService, CancelacionService>();
builder.Services.AddScoped<IReporteService, ReporteService>();
builder.Services.AddScoped<ICredencialesService, CredencialesService>();
builder.Services.AddScoped<ISolicitudRepository, SolicitudRepository>();
builder.Services.AddScoped<ISolicitudService, SolicitudService>();
builder.Services.AddScoped<IServicioRepository, ServicioRepository>();
builder.Services.AddScoped<IServicioService, ServicioService>();
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<IRaquetaRepository, RaquetaRepository>();
builder.Services.AddScoped<IRaquetaService, RaquetaService>();
builder.Services.AddScoped<ISolicitudGrupoRepository, SolicitudGrupoRepository>();
builder.Services.AddScoped<ISolicitudGrupoService, SolicitudGrupoService>();

// Base de datos: EF Core sobre SQLite (la connection string vive en appsettings.json)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// ── Auth (ADR-0007) ──
// Identity CORE (sin roles de Identity: los roles son membresías por tenant).
// Aporta hashing de contraseñas, validaciones y UserManager.
builder.Services.AddIdentityCore<Usuario>(options =>
{
    // Prototipo: solo mínimo de largo (coincide con lo que valida el front,
    // y permite usar el teléfono del alumno como contraseña inicial —
    // sin exigir mayúscula/minúscula/dígito). Endurecer al desplegar.
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireDigit = false;
    options.User.RequireUniqueEmail = true;
})
.AddErrorDescriber<SistemaIntegralDeportivo.Api.Common.IdentityErroresEnEspanol>()
.AddEntityFrameworkStores<AppDbContext>();

// JWT Bearer: el front manda el token en Authorization; acá se valida
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta Jwt:Key en la configuración.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Los claims llegan tal como se emitieron ("sub", no el URI de .NET)
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
        };
    });

// Policy "Profesor": el claim lo emite TokenService si es dueño de un tenant
builder.Services.AddAuthorization(options =>
    options.AddPolicy("Profesor", p => p.RequireClaim("profesor", "true")));

// CORS: el front (Vite, puerto 5173) corre en otro origen que esta API,
// y el navegador bloquea esas llamadas salvo que las permitamos explícitamente.
// Solo en desarrollo; la política de producción se define al desplegar.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed de auth: el profe dueño del tenant demo (idempotente)
using (var scope = app.Services.CreateScope())
    await AuthSeeder.SeedAsync(scope.ServiceProvider);

app.Run();
