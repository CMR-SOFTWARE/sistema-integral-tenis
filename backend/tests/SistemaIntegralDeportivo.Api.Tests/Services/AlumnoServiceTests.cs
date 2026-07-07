using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Tests de la lógica de negocio del alta de alumno (TDD, ADR-0005).
/// El repositorio está mockeado: acá NO hay base de datos, solo reglas.
/// </summary>
public class AlumnoServiceTests
{
    private readonly Mock<IAlumnoRepository> _repo;
    private readonly AlumnoService _service;

    public AlumnoServiceTests()
    {
        _repo = new Mock<IAlumnoRepository>();

        // Por defecto: el DNI no existe y AgregarAsync devuelve lo que recibe
        _repo.Setup(r => r.ExisteDniAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Alumno a, CancellationToken _) => a);

        _service = new AlumnoService(_repo.Object);
    }

    /// <summary>DTO válido de un alumno MAYOR de edad (base de los tests).</summary>
    private static CreateAlumnoDto AlumnoMayor() => new()
    {
        Nombre = "Juan",
        Apellido = "Pérez",
        Dni = "30111222",
        Telefono = "+5491155551234",
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

        // Y no tiene que haber intentado persistir nada
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()), Times.Never);
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

        Assert.True(result.EsMenor);
        Assert.Equal("Sofía", result.Nombre);
        _repo.Verify(r => r.AgregarAsync(
            It.Is<Alumno>(a => a.Tutor != null && a.Tutor.Dni == "22555666"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CrearAsync_MayorSinTutor_CreaSinProblema()
    {
        var result = await _service.CrearAsync(AlumnoMayor());

        Assert.False(result.EsMenor);
        Assert.Equal("Activo", result.Estado); // nace activo
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────
    // Consentimiento con timestamp (no alcanza el bool)
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

    // ─────────────────────────────────────────────
    // Unicidad de DNI por tenant
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CrearAsync_DniYaExistenteEnElTenant_LanzaReglaDeNegocio()
    {
        _repo.Setup(r => r.ExisteDniAsync("30111222", It.IsAny<CancellationToken>()))
             .ReturnsAsync(true); // ya hay un alumno con ese DNI

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(AlumnoMayor()));

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
