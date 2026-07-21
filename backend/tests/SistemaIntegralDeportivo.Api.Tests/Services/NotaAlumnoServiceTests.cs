using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Notas del profe por alumno (seguimiento, TDD). La regla crítica: el alumno ve
/// SOLO las compartidas, nunca las privadas. Además, no se le carga una nota a un
/// alumno que no es del club. El resto es crear/listar/borrar.
/// </summary>
public class NotaAlumnoServiceTests
{
    private static readonly Guid AlumnoId = Guid.NewGuid();

    private readonly Mock<INotaAlumnoRepository> _notas;
    private readonly Mock<IAlumnoRepository> _alumnos;
    private readonly NotaAlumnoService _service;

    public NotaAlumnoServiceTests()
    {
        _notas = new Mock<INotaAlumnoRepository>();
        _alumnos = new Mock<IAlumnoRepository>();
        _service = new NotaAlumnoService(_notas.Object, _alumnos.Object);

        // Por defecto, el alumno existe en el club.
        _alumnos.Setup(a => a.ObtenerAsync(AlumnoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Alumno
                {
                    Id = AlumnoId, Nombre = "Lucas", Apellido = "C", Dni = "1", Telefono = "1",
                    FechaNacimiento = DateTime.UtcNow.AddYears(-20),
                });
    }

    private static NotaAlumno Nota(bool compartida) => new()
    {
        AlumnoId = AlumnoId, Texto = compartida ? "Buen revés" : "Le cuesta el saque", Compartida = compartida,
    };

    [Fact]
    public async Task Crear_CasoFeliz_GuardaLaNota()
    {
        NotaAlumno? creada = null;
        _notas.Setup(r => r.AgregarAsync(It.IsAny<NotaAlumno>(), It.IsAny<CancellationToken>()))
              .Callback((NotaAlumno n, CancellationToken _) => creada = n).Returns(Task.CompletedTask);

        var dto = new CrearNotaAlumnoDto { Texto = "Mejoró la derecha", Compartida = true };
        var res = await _service.CrearAsync(AlumnoId, dto);

        Assert.NotNull(creada);
        Assert.Equal(AlumnoId, creada!.AlumnoId);
        Assert.Equal("Mejoró la derecha", creada.Texto);
        Assert.True(creada.Compartida);
        Assert.True(res.Compartida);
    }

    [Fact]
    public async Task Crear_AlumnoDeOtroClub_Lanza()
    {
        // El repo (tenant-scoped) no encuentra al alumno → no es del club.
        _alumnos.Setup(a => a.ObtenerAsync(AlumnoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Alumno?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(AlumnoId, new CrearNotaAlumnoDto { Texto = "x" }));

        _notas.Verify(r => r.AgregarAsync(It.IsAny<NotaAlumno>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Listar_ParaAlumno_DevuelveSoloLasCompartidas()
    {
        _notas.Setup(r => r.ListarPorAlumnoAsync(AlumnoId, It.IsAny<CancellationToken>()))
              .ReturnsAsync([Nota(compartida: true), Nota(compartida: false), Nota(compartida: true)]);

        var res = await _service.ListarAsync(AlumnoId, soloCompartidas: true);

        Assert.Equal(2, res.Count);
        Assert.All(res, n => Assert.True(n.Compartida));
    }

    [Fact]
    public async Task Listar_ParaProfe_DevuelveTodas()
    {
        _notas.Setup(r => r.ListarPorAlumnoAsync(AlumnoId, It.IsAny<CancellationToken>()))
              .ReturnsAsync([Nota(compartida: true), Nota(compartida: false)]);

        var res = await _service.ListarAsync(AlumnoId, soloCompartidas: false);

        Assert.Equal(2, res.Count);
    }

    [Fact]
    public async Task Eliminar_BorraLaNota()
    {
        var nota = Nota(compartida: false);
        _notas.Setup(r => r.ObtenerAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);

        await _service.EliminarAsync(nota.Id);

        _notas.Verify(r => r.Eliminar(nota), Times.Once);
        _notas.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Eliminar_Inexistente_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.EliminarAsync(Guid.NewGuid()));
    }
}
