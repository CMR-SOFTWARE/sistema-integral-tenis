using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Lista de espera (TDD): unirse a un club crea la ficha DIRECTO en espera
/// (EnEspera), sin aprobar; un club por persona y datos completos. El profe ve
/// la lista, puede quitar, y la persona se vuelve alumno al recibir una clase.
/// </summary>
public class SolicitudServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly Mock<IAlumnoService> _alumnos;
    private readonly Mock<IAlumnoRepository> _alumnoRepo;
    private readonly Mock<ITenantRepository> _tenants;
    private readonly Mock<ITenantActual> _tenantActual;
    private readonly Mock<IUsuarioActual> _usuario;
    private readonly SolicitudService _service;

    public SolicitudServiceTests()
    {
        _alumnos = new Mock<IAlumnoService>();
        _alumnoRepo = new Mock<IAlumnoRepository>();
        _tenants = new Mock<ITenantRepository>();
        _tenantActual = new Mock<ITenantActual>();
        _usuario = new Mock<IUsuarioActual>(); // por defecto: no es staff (dueño ve toda la espera)
        _service = new SolicitudService(
            _alumnos.Object, _alumnoRepo.Object, _tenants.Object, _tenantActual.Object, _usuario.Object);

        // Por defecto: el club existe y está activo, sin ficha previa
        _tenants.Setup(t => t.ObtenerPorIdAsync(TenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClubActivo());
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

    // ── Unirse (crea la ficha directo en la lista de espera) ──

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
    public async Task Crear_YaEstoyEnUnClub_Lanza()
    {
        // Un club por persona POR AHORA (multi-club llega con la reserva de turnos)
        _alumnoRepo.Setup(a => a.ObtenerPorUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Alumno
                   {
                       Nombre = "L", Apellido = "C", Dni = "1", Telefono = "+5",
                       UserId = UserId,
                   });

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(Jugador(), new CrearSolicitudDto { TenantId = TenantId }));
        _alumnos.Verify(a => a.CrearVinculadoAsync(
            It.IsAny<CreateAlumnoDto>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
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
    public async Task Crear_OK_CreaLaFichaVinculadaEnEseClubConLosDatos()
    {
        CreateAlumnoDto? dtoUsado = null;
        _alumnos.Setup(a => a.CrearVinculadoAsync(It.IsAny<CreateAlumnoDto>(), UserId, It.IsAny<CancellationToken>()))
                .Callback((CreateAlumnoDto d, Guid _, CancellationToken _) => dtoUsado = d)
                .ReturnsAsync(new AlumnoResponseDto { Id = Guid.NewGuid(), Nombre = "Lucas" });

        await _service.CrearAsync(Jugador(), new CrearSolicitudDto
        {
            TenantId = TenantId, Mensaje = "Juego los martes",
        });

        // Fija el club elegido y crea la ficha con los datos del registro
        _tenantActual.Verify(t => t.Establecer(TenantId), Times.Once);
        Assert.NotNull(dtoUsado);
        Assert.Equal("Lucas", dtoUsado!.Nombre);
        Assert.Equal("30111222", dtoUsado.Dni);
        Assert.Equal("lucas@mail.com", dtoUsado.Email);
        Assert.Equal(CategoriaAlumno.Cuarta, dtoUsado.Categoria);
        Assert.True(dtoUsado.ConsentimientoDatos);       // lo dio al registrarse
        Assert.Equal("Juego los martes", dtoUsado.Notas); // el mensaje queda como nota
    }

    // ── Lista de espera del profe + quitar ──

    [Fact]
    public async Task Pendientes_DevuelveLasFichasEnEspera()
    {
        _alumnoRepo.Setup(a => a.ListarAsync(null, EstadoAlumno.EnEspera, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new[]
                   {
                       new Alumno { Nombre = "Ana", Apellido = "Mora", Dni = "40", Telefono = "+1",
                                    Estado = EstadoAlumno.EnEspera, Notas = "Los jueves" },
                   });

        var lista = await _service.PendientesAsync();

        var item = Assert.Single(lista);
        Assert.Equal("Ana", item.Nombre);
        Assert.Equal("Los jueves", item.Mensaje);
    }

    [Fact]
    public async Task Pendientes_Staff_SoloVeLosSuyos()
    {
        var yo = Guid.NewGuid();
        _usuario.Setup(u => u.EsStaff).Returns(true);
        _usuario.Setup(u => u.UserId).Returns(yo);
        _alumnoRepo.Setup(a => a.ListarAsync(null, EstadoAlumno.EnEspera, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new[]
                   {
                       new Alumno { Nombre = "Mío", Apellido = "A", Dni = "1", Telefono = "+1",
                                    Estado = EstadoAlumno.EnEspera, ProfesorUserId = yo },
                       new Alumno { Nombre = "Ajeno", Apellido = "B", Dni = "2", Telefono = "+2",
                                    Estado = EstadoAlumno.EnEspera, ProfesorUserId = Guid.NewGuid() },
                   });

        var lista = await _service.PendientesAsync();

        var item = Assert.Single(lista);
        Assert.Equal("Mío", item.Nombre);
    }

    [Fact]
    public async Task Quitar_Staff_FichaDeOtroProfe_Lanza()
    {
        var yo = Guid.NewGuid();
        _usuario.Setup(u => u.EsStaff).Returns(true);
        _usuario.Setup(u => u.UserId).Returns(yo);
        var ajena = new Alumno
        {
            Nombre = "Ajeno", Apellido = "B", Dni = "2", Telefono = "+2",
            Estado = EstadoAlumno.EnEspera, ProfesorUserId = Guid.NewGuid(),
        };
        _alumnoRepo.Setup(a => a.ObtenerAsync(ajena.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ajena);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.QuitarDeEsperaAsync(ajena.Id));
        _alumnoRepo.Verify(a => a.EliminarDefinitivoAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Quitar_EnEspera_BorraLaFicha()
    {
        var ficha = new Alumno
        {
            Nombre = "Ana", Apellido = "Mora", Dni = "40", Telefono = "+1",
            Estado = EstadoAlumno.EnEspera,
        };
        _alumnoRepo.Setup(a => a.ObtenerAsync(ficha.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);

        await _service.QuitarDeEsperaAsync(ficha.Id);

        _alumnoRepo.Verify(a => a.EliminarDefinitivoAsync(ficha, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Quitar_NoEstaEnEspera_Lanza()
    {
        var ficha = new Alumno
        {
            Nombre = "Ana", Apellido = "Mora", Dni = "40", Telefono = "+1",
            Estado = EstadoAlumno.Activo, // ya es alumno de verdad: no se quita desde acá
        };
        _alumnoRepo.Setup(a => a.ObtenerAsync(ficha.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.QuitarDeEsperaAsync(ficha.Id));
        _alumnoRepo.Verify(a => a.EliminarDefinitivoAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
