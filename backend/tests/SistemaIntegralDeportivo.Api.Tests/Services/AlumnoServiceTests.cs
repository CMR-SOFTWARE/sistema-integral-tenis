using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Tests de la lógica de negocio del alta de alumno (TDD, ADR-0005).
/// Plan v2: el alta crea TAMBIÉN las credenciales (usuario + temporal) —
/// el registro es una sola vez. El repositorio e Identity están mockeados.
/// </summary>
public class AlumnoServiceTests
{
    private static readonly Guid UserIdNuevo = Guid.NewGuid();

    private readonly Mock<IAlumnoRepository> _repo;
    private readonly Mock<ICargoRepository> _cargos;
    private readonly Mock<ICredencialesService> _credenciales;
    private readonly AlumnoService _service;

    public AlumnoServiceTests()
    {
        _repo = new Mock<IAlumnoRepository>();
        _cargos = new Mock<ICargoRepository>();
        _credenciales = new Mock<ICredencialesService>();

        // Por defecto: el DNI no existe y AgregarAsync devuelve lo que recibe
        _repo.Setup(r => r.ExisteDniAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);
        _repo.Setup(r => r.ObtenerPorDniAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Alumno?)null);
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Alumno a, CancellationToken _) => a);

        // Por defecto: nadie debe nada y las credenciales salen bien
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        _credenciales.Setup(c => c.CrearConTemporalAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new CredencialesCreadas(UserIdNuevo, "Temp1234AB"));

        _service = new AlumnoService(_repo.Object, _cargos.Object, _credenciales.Object);
    }

    /// <summary>DTO válido de un alumno MAYOR de edad (base de los tests).</summary>
    private static CreateAlumnoDto AlumnoMayor() => new()
    {
        Nombre = "Juan",
        Apellido = "Pérez",
        Dni = "30111222",
        Telefono = "+5491155551234",
        Email = "juan@mail.com",
        FechaNacimiento = DateTime.UtcNow.AddYears(-30), // 30 años
        Categoria = CategoriaAlumno.Cuarta,
        ConsentimientoDatos = true,
    };

    /// <summary>DTO de un alumno MENOR (15 años), sin tutor por defecto.</summary>
    private static CreateAlumnoDto AlumnoMenor() => new()
    {
        Nombre = "Sofía",
        Apellido = "Gómez",
        Dni = "50333444",
        Telefono = "+5491155559876",
        Email = "sofia@mail.com",
        FechaNacimiento = DateTime.UtcNow.AddYears(-15), // 15 años
        Categoria = CategoriaAlumno.Septima,
    };

    private static TutorDto TutorValido() => new()
    {
        Nombre = "Marta",
        Apellido = "Gómez",
        Dni = "22555666",
        Telefono = "+5491144443333",
        Relacion = RelacionTutor.Madre,
    };

    // ─────────────────────────────────────────────
    // Regla del menor (modelo-alumnos.md §3.2, Ley 25.326)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CrearAsync_MenorSinTutor_LanzaReglaDeNegocio()
    {
        var dto = AlumnoMenor(); // sin tutor
        dto.ConsentimientoDatos = true;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));

        // Y no tiene que haber intentado persistir NI crear usuario
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()), Times.Never);
        _credenciales.Verify(c => c.CrearConTemporalAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CrearAsync_MenorSinConsentimientoDeDatos_LanzaReglaDeNegocio()
    {
        var dto = AlumnoMenor();
        dto.Tutor = TutorValido();
        dto.ConsentimientoDatos = false; // el tutor NO consintió

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
    }

    [Fact]
    public async Task CrearAsync_MenorConTutorYConsentimiento_CreaYMarcaEsMenor()
    {
        var dto = AlumnoMenor();
        dto.Tutor = TutorValido();
        dto.ConsentimientoDatos = true;

        var result = await _service.CrearAsync(dto);

        Assert.True(result.Alumno.EsMenor);
        Assert.Equal("Sofía", result.Alumno.Nombre);
        _repo.Verify(r => r.AgregarAsync(
            It.Is<Alumno>(a => a.Tutor != null && a.Tutor.Dni == "22555666"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────
    // Credenciales: el alta crea el usuario con temporal (registro único)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CrearAsync_CreaLasCredenciales_YDevuelveLaTemporalUnaVez()
    {
        var result = await _service.CrearAsync(AlumnoMayor());

        Assert.Equal("Temp1234AB", result.PasswordTemporal);
        Assert.Equal("juan@mail.com", result.Email);
        // La ficha nace VINCULADA al usuario nuevo
        _repo.Verify(r => r.AgregarAsync(
            It.Is<Alumno>(a => a.UserId == UserIdNuevo),
            It.IsAny<CancellationToken>()), Times.Once);
        _credenciales.Verify(c => c.CrearConTemporalAsync(
            "juan@mail.com", "Juan", "Pérez", "30111222", "+5491155551234",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CrearAsync_EmailYaRegistrado_Lanza_YNoPersisteLaFicha()
    {
        _credenciales.Setup(c => c.CrearConTemporalAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new ReglaDeNegocioException("El email ya tiene una cuenta."));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(AlumnoMayor()));

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CrearAsync_DniDuplicado_NoLlegaACrearElUsuario()
    {
        // Orden anti-huérfanos: las reglas de la ficha se validan ANTES de Identity
        _repo.Setup(r => r.ExisteDniAsync("30111222", It.IsAny<CancellationToken>()))
             .ReturnsAsync(true);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(AlumnoMayor()));

        _credenciales.Verify(c => c.CrearConTemporalAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CrearAsync_SiFallaLaFicha_BorraElUsuarioCreado()
    {
        // Compensación: si la ficha explota (carrera del índice único), el
        // usuario recién creado no puede quedar huérfano
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new InvalidOperationException("índice único"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CrearAsync(AlumnoMayor()));

        _credenciales.Verify(c => c.EliminarAsync(UserIdNuevo, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────
    // Crear acceso (fichas viejas sin usuario)
    // ─────────────────────────────────────────────

    private Alumno FichaExistente(string? email = "juan@mail.com", Guid? userId = null)
    {
        var alumno = new Alumno
        {
            Nombre = "Juan", Apellido = "Pérez", Dni = "30111222",
            Telefono = "+5491155551234", Email = email,
            FechaNacimiento = DateTime.UtcNow.AddYears(-30),
            UserId = userId,
        };
        _repo.Setup(r => r.ObtenerAsync(alumno.Id, It.IsAny<CancellationToken>()))
             .ReturnsAsync(alumno);
        return alumno;
    }

    [Fact]
    public async Task CrearAcceso_FichaSinUsuario_VinculaYDevuelveLaTemporal()
    {
        var ficha = FichaExistente();

        var acceso = await _service.CrearAccesoAsync(ficha.Id, email: null);

        Assert.Equal("Temp1234AB", acceso.PasswordTemporal);
        Assert.Equal("juan@mail.com", acceso.Email); // usó el email de la ficha
        Assert.Equal(UserIdNuevo, ficha.UserId);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CrearAcceso_FichaSinEmail_ExigeElEmailDelBody_YLoGuarda()
    {
        var ficha = FichaExistente(email: null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAccesoAsync(ficha.Id, email: null));

        var acceso = await _service.CrearAccesoAsync(ficha.Id, email: "nuevo@mail.com");
        Assert.Equal("nuevo@mail.com", acceso.Email);
        Assert.Equal("nuevo@mail.com", ficha.Email); // quedó en la ficha
    }

    [Fact]
    public async Task CrearAcceso_FichaYaConUsuario_Lanza()
    {
        var ficha = FichaExistente(userId: Guid.NewGuid());

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAccesoAsync(ficha.Id, email: null));
    }

    // ─────────────────────────────────────────────
    // Crear VINCULADO (aprobación de solicitudes): sin credenciales nuevas
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CrearVinculado_CreaLaFichaConElUserId_SinTocarIdentity()
    {
        var userId = Guid.NewGuid();

        var creado = await _service.CrearVinculadoAsync(AlumnoMayor(), userId);

        Assert.Equal("Juan", creado.Nombre);
        _repo.Verify(r => r.AgregarAsync(
            It.Is<Alumno>(a => a.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);
        _credenciales.Verify(c => c.CrearConTemporalAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CrearVinculado_DniExistenteSinUsuario_VinculaEsaFicha_SinDuplicar()
    {
        // El profe ya lo tenía cargado: el reemplazo elegante del reclamo
        var userId = Guid.NewGuid();
        var existente = FichaExistente();
        _repo.Setup(r => r.ObtenerPorDniAsync("30111222", It.IsAny<CancellationToken>()))
             .ReturnsAsync(existente);

        var creado = await _service.CrearVinculadoAsync(AlumnoMayor(), userId);

        Assert.Equal(existente.Id, creado.Id);
        Assert.Equal(userId, existente.UserId);
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CrearVinculado_DniDeOtraCuenta_Lanza()
    {
        var existente = FichaExistente(userId: Guid.NewGuid()); // ya es de otro
        _repo.Setup(r => r.ObtenerPorDniAsync("30111222", It.IsAny<CancellationToken>()))
             .ReturnsAsync(existente);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearVinculadoAsync(AlumnoMayor(), Guid.NewGuid()));
    }

    // ─────────────────────────────────────────────
    // Consentimiento con timestamp + unicidad de DNI (reglas previas)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CrearAsync_ConConsentimientos_GuardaCuandoSeOtorgaron()
    {
        var dto = AlumnoMayor();
        dto.ConsentimientoWhatsapp = true;

        Alumno? persistido = null;
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()))
             .Callback((Alumno a, CancellationToken _) => persistido = a)
             .ReturnsAsync((Alumno a, CancellationToken _) => a);

        await _service.CrearAsync(dto);

        Assert.NotNull(persistido);
        Assert.NotNull(persistido!.ConsentimientoDatosEl);    // quedó el timestamp
        Assert.NotNull(persistido.ConsentimientoWhatsappEl);
    }

    [Fact]
    public async Task CrearAsync_DniYaExistenteEnElTenant_LanzaReglaDeNegocio()
    {
        _repo.Setup(r => r.ExisteDniAsync("30111222", It.IsAny<CancellationToken>()))
             .ReturnsAsync(true); // ya hay un alumno con ese DNI

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(AlumnoMayor()));

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
