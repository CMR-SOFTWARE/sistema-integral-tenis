using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Reglas de solicitudes alumno→profe (TDD, plan v2 — reemplaza al reclamo):
/// una pendiente por club, un club por alumno (por ahora), y aprobar crea
/// o vincula la ficha con los datos del registro.
/// </summary>
public class SolicitudServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly Mock<ISolicitudRepository> _solicitudes;
    private readonly Mock<IAlumnoService> _alumnos;
    private readonly Mock<IAlumnoRepository> _alumnoRepo;
    private readonly Mock<ITenantRepository> _tenants;
    private readonly SolicitudService _service;

    public SolicitudServiceTests()
    {
        _solicitudes = new Mock<ISolicitudRepository>();
        _alumnos = new Mock<IAlumnoService>();
        _alumnoRepo = new Mock<IAlumnoRepository>();
        _tenants = new Mock<ITenantRepository>();
        _service = new SolicitudService(
            _solicitudes.Object, _alumnos.Object, _alumnoRepo.Object, _tenants.Object);

        // Por defecto: el club existe y está activo, sin pendientes ni ficha previa
        _tenants.Setup(t => t.ObtenerPorIdAsync(TenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClubActivo());
        _solicitudes.Setup(s => s.ExistePendienteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
        _solicitudes.Setup(s => s.ListarPorUsuarioAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync([]);
        _alumnoRepo.Setup(a => a.ObtenerPorUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Alumno?)null);
    }

    private static Tenant ClubActivo() => new()
    {
        Id = TenantId, Subdominio = "club-x", Nombre = "Club X",
        Estado = EstadoTenant.Activo,
    };

    /// <summary>Jugador con datos completos (registro segmentado C1).</summary>
    private static Usuario Jugador() => new()
    {
        Id = UserId,
        UserName = "lucas@mail.com",
        Email = "lucas@mail.com",
        Nombre = "Lucas",
        Apellido = "Calderón",
        Dni = "30111222",
        PhoneNumber = "+549115555",
        FechaNacimiento = DateTime.UtcNow.AddYears(-30),
        Categoria = CategoriaAlumno.Cuarta,
    };

    // ─────────────────────────────────────────────
    // Crear solicitud
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Crear_ClubInexistente_Lanza()
    {
        _tenants.Setup(t => t.ObtenerPorIdAsync(TenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tenant?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(Jugador(), new CrearSolicitudDto { TenantId = TenantId }));
    }

    [Fact]
    public async Task Crear_ClubNoActivo_Lanza()
    {
        var club = ClubActivo();
        club.Estado = EstadoTenant.PendientePago;
        _tenants.Setup(t => t.ObtenerPorIdAsync(TenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(club);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(Jugador(), new CrearSolicitudDto { TenantId = TenantId }));
    }

    [Fact]
    public async Task Crear_YaTengoSolicitudPendienteEnEseClub_Lanza()
    {
        _solicitudes.Setup(s => s.ExistePendienteAsync(UserId, TenantId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(Jugador(), new CrearSolicitudDto { TenantId = TenantId }));

        _solicitudes.Verify(s => s.AgregarAsync(It.IsAny<Solicitud>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Crear_YaEstoyEnUnClub_Lanza()
    {
        // Un club por alumno POR AHORA (multi-club llega con la reserva de turnos)
        _alumnoRepo.Setup(a => a.ObtenerPorUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Alumno
                   {
                       Nombre = "L", Apellido = "C", Dni = "1", Telefono = "+5",
                       UserId = UserId,
                   });

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(Jugador(), new CrearSolicitudDto { TenantId = TenantId }));
    }

    [Fact]
    public async Task Crear_SinDatosCompletos_Lanza()
    {
        // Cuentas viejas sin fecha de nacimiento: pedirle que complete el perfil
        var usuario = Jugador();
        usuario.FechaNacimiento = null;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(usuario, new CrearSolicitudDto { TenantId = TenantId }));
    }

    [Fact]
    public async Task Crear_OK_NacePendienteConMensaje()
    {
        Solicitud? creada = null;
        _solicitudes.Setup(s => s.AgregarAsync(It.IsAny<Solicitud>(), It.IsAny<CancellationToken>()))
                    .Callback((Solicitud s, CancellationToken _) => creada = s)
                    .Returns(Task.CompletedTask);

        await _service.CrearAsync(Jugador(), new CrearSolicitudDto
        {
            TenantId = TenantId, Mensaje = "Juego los martes",
        });

        Assert.NotNull(creada);
        Assert.Equal(EstadoSolicitud.Pendiente, creada!.Estado);
        Assert.Equal(UserId, creada.UserId);
        Assert.Equal(TenantId, creada.TenantId);
        Assert.Equal("Juego los martes", creada.Mensaje);
        _solicitudes.Verify(s => s.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────
    // Aprobar / rechazar (lado profe)
    // ─────────────────────────────────────────────

    private Solicitud Pendiente()
    {
        var solicitud = new Solicitud { UserId = UserId, TenantId = TenantId };
        _solicitudes.Setup(s => s.ObtenerPendienteConUsuarioAsync(solicitud.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((solicitud, Jugador()));
        return solicitud;
    }

    [Fact]
    public async Task Aprobar_CreaLaFichaVinculadaConLosDatosDelRegistro_YMarcaAprobada()
    {
        var solicitud = Pendiente();
        var ficha = new AlumnoResponseDto { Id = Guid.NewGuid(), Nombre = "Lucas" };
        CreateAlumnoDto? dtoUsado = null;
        _alumnos.Setup(a => a.CrearVinculadoAsync(It.IsAny<CreateAlumnoDto>(), UserId, It.IsAny<CancellationToken>()))
                .Callback((CreateAlumnoDto d, Guid _, CancellationToken _) => dtoUsado = d)
                .ReturnsAsync(ficha);

        var resultado = await _service.AprobarAsync(solicitud.Id);

        Assert.Equal(ficha.Id, resultado.Id);
        Assert.Equal(EstadoSolicitud.Aprobada, solicitud.Estado);
        Assert.Equal(ficha.Id, solicitud.AlumnoId);
        Assert.NotNull(solicitud.ResueltoEl);
        // Los datos del registro viajan a la ficha
        Assert.NotNull(dtoUsado);
        Assert.Equal("Lucas", dtoUsado!.Nombre);
        Assert.Equal("30111222", dtoUsado.Dni);
        Assert.Equal("lucas@mail.com", dtoUsado.Email);
        Assert.Equal(CategoriaAlumno.Cuarta, dtoUsado.Categoria);
        Assert.True(dtoUsado.ConsentimientoDatos); // lo dio al registrarse
        _solicitudes.Verify(s => s.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Aprobar_InexistenteODeOtroClub_Lanza()
    {
        _solicitudes.Setup(s => s.ObtenerPendienteConUsuarioAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(((Solicitud, Usuario)?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AprobarAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Aprobar_SiLaFichaFalla_LaSolicitudQuedaPendiente()
    {
        // Ej.: menor sin tutor → el profe lo carga a mano desde Alumnos
        var solicitud = Pendiente();
        _alumnos.Setup(a => a.CrearVinculadoAsync(It.IsAny<CreateAlumnoDto>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ReglaDeNegocioException("Un alumno menor de edad requiere un tutor."));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.AprobarAsync(solicitud.Id));

        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
        _solicitudes.Verify(s => s.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Rechazar_SoloMarca_NuncaTocaAlumnos()
    {
        var solicitud = Pendiente();

        await _service.RechazarAsync(solicitud.Id);

        Assert.Equal(EstadoSolicitud.Rechazada, solicitud.Estado);
        Assert.NotNull(solicitud.ResueltoEl);
        _alumnos.Verify(a => a.CrearVinculadoAsync(It.IsAny<CreateAlumnoDto>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _solicitudes.Verify(s => s.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
