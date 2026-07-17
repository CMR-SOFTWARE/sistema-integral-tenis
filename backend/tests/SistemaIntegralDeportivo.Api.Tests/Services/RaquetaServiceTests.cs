using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Raquetas del alumno (M3, TDD): la regla es la PERTENENCIA — solo puedo
/// editar/borrar una raqueta que sea mía.
/// </summary>
public class RaquetaServiceTests
{
    private static readonly Guid Yo = Guid.NewGuid();
    private static readonly Guid Otro = Guid.NewGuid();

    private readonly Mock<IRaquetaRepository> _repo;
    private readonly RaquetaService _service;

    public RaquetaServiceTests()
    {
        _repo = new Mock<IRaquetaRepository>();
        _service = new RaquetaService(_repo.Object);
    }

    private Raqueta RaquetaDe(Guid alumnoId)
    {
        var r = new Raqueta { AlumnoId = alumnoId, Marca = "Wilson Blade" };
        _repo.Setup(x => x.ObtenerAsync(r.Id, It.IsAny<CancellationToken>())).ReturnsAsync(r);
        return r;
    }

    [Fact]
    public async Task Agregar_CreaLaRaqueta_ConSusDatos()
    {
        Raqueta? creada = null;
        _repo.Setup(x => x.AgregarAsync(It.IsAny<Raqueta>(), It.IsAny<CancellationToken>()))
             .Callback((Raqueta r, CancellationToken _) => creada = r)
             .Returns(Task.CompletedTask);

        var dto = new GuardarRaquetaDto { Marca = "Babolat Pure Aero", Tension = "24 kg", MarcaEncordado = "RPM Blast" };
        var res = await _service.AgregarAsync(Yo, dto);

        Assert.NotNull(creada);
        Assert.Equal(Yo, creada!.AlumnoId);
        Assert.Equal("Babolat Pure Aero", creada.Marca);
        Assert.Equal("24 kg", creada.Tension);
        Assert.Equal("RPM Blast", creada.MarcaEncordado);
        Assert.Equal("Babolat Pure Aero", res.Marca);
    }

    [Fact]
    public async Task Editar_RaquetaMia_ActualizaLosDatos()
    {
        var raqueta = RaquetaDe(Yo);

        await _service.EditarAsync(Yo, raqueta.Id, new GuardarRaquetaDto { Marca = "Head Speed", Tension = "23 kg" });

        Assert.Equal("Head Speed", raqueta.Marca);
        Assert.Equal("23 kg", raqueta.Tension);
        _repo.Verify(x => x.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Editar_RaquetaDeOtro_Lanza()
    {
        var ajena = RaquetaDe(Otro);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.EditarAsync(Yo, ajena.Id, new GuardarRaquetaDto { Marca = "X" }));

        Assert.Equal("Wilson Blade", ajena.Marca); // no se tocó
    }

    [Fact]
    public async Task Editar_RaquetaInexistente_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.EditarAsync(Yo, Guid.NewGuid(), new GuardarRaquetaDto { Marca = "X" }));
    }

    [Fact]
    public async Task Borrar_RaquetaMia_LaElimina()
    {
        var raqueta = RaquetaDe(Yo);

        await _service.BorrarAsync(Yo, raqueta.Id);

        _repo.Verify(x => x.Eliminar(raqueta), Times.Once);
        _repo.Verify(x => x.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Borrar_RaquetaDeOtro_Lanza_YNoElimina()
    {
        var ajena = RaquetaDe(Otro);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.BorrarAsync(Yo, ajena.Id));

        _repo.Verify(x => x.Eliminar(It.IsAny<Raqueta>()), Times.Never);
    }
}
