using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Avisos generales del profe (tablón del club, TDD). Reglas: no se crea un aviso
/// con vencimiento ya pasado, y la vista del alumno (soloVigentes) oculta los
/// vencidos y los apagados. El resto es CRUD + baja/reactivación.
/// </summary>
public class AvisoServiceTests
{
    private readonly Mock<IAvisoRepository> _repo;
    private readonly AvisoService _service;

    public AvisoServiceTests()
    {
        _repo = new Mock<IAvisoRepository>();
        _service = new AvisoService(_repo.Object);
    }

    private static GuardarAvisoDto Dto(DateOnly? vence = null) => new()
    {
        Titulo = "Sin clases el viernes",
        Mensaje = "Se suspende por el torneo interno.",
        VenceEl = vence,
    };

    private static Aviso Aviso(bool activo = true, DateOnly? vence = null) => new()
    {
        Titulo = "x", Mensaje = "y", Activo = activo, VenceEl = vence,
    };

    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Crear_CasoFeliz_CreaElAvisoActivo()
    {
        Aviso? creado = null;
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Aviso>(), It.IsAny<CancellationToken>()))
             .Callback((Aviso a, CancellationToken _) => creado = a).Returns(Task.CompletedTask);

        var res = await _service.CrearAsync(Dto(Hoy.AddDays(3)));

        Assert.NotNull(creado);
        Assert.True(creado!.Activo);
        Assert.Equal("Sin clases el viernes", creado.Titulo);
        Assert.True(res.Activo);
    }

    [Fact]
    public async Task Crear_SinVencimiento_EsValido()
    {
        var res = await _service.CrearAsync(Dto(vence: null));

        Assert.Null(res.VenceEl);
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Aviso>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crear_VencimientoPasado_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(Dto(Hoy.AddDays(-1))));

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Aviso>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Listar_SoloVigentes_OcultaLosVencidos()
    {
        // El repo (soloActivos) devuelve activos; el service filtra por vencimiento.
        var vigente = Aviso(vence: Hoy.AddDays(2));
        var sinVence = Aviso(vence: null);
        var vencido = Aviso(vence: Hoy.AddDays(-1));
        _repo.Setup(r => r.ListarAsync(true, It.IsAny<CancellationToken>()))
             .ReturnsAsync([vigente, sinVence, vencido]);

        var res = await _service.ListarAsync(soloVigentes: true);

        Assert.Equal(2, res.Count);
        Assert.DoesNotContain(res, a => a.Id == vencido.Id);
    }

    [Fact]
    public async Task Listar_VenceHoy_SigueVigente()
    {
        _repo.Setup(r => r.ListarAsync(true, It.IsAny<CancellationToken>()))
             .ReturnsAsync([Aviso(vence: Hoy)]);

        var res = await _service.ListarAsync(soloVigentes: true);

        Assert.Single(res); // vence al final del día de hoy: todavía se ve
    }

    [Fact]
    public async Task Listar_ParaProfe_TraeTodosSinFiltrarVencimiento()
    {
        // El profe (soloVigentes=false) ve todo para poder gestionarlo.
        _repo.Setup(r => r.ListarAsync(false, It.IsAny<CancellationToken>()))
             .ReturnsAsync([Aviso(vence: Hoy.AddDays(-5)), Aviso(activo: false)]);

        var res = await _service.ListarAsync(soloVigentes: false);

        Assert.Equal(2, res.Count);
    }

    [Fact]
    public async Task CambiarActivo_ApagaElAviso()
    {
        var aviso = Aviso(activo: true);
        _repo.Setup(r => r.ObtenerAsync(aviso.Id, It.IsAny<CancellationToken>())).ReturnsAsync(aviso);

        await _service.CambiarActivoAsync(aviso.Id, false);

        Assert.False(aviso.Activo);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Eliminar_BorraElAviso()
    {
        var aviso = Aviso();
        _repo.Setup(r => r.ObtenerAsync(aviso.Id, It.IsAny<CancellationToken>())).ReturnsAsync(aviso);

        await _service.EliminarAsync(aviso.Id);

        _repo.Verify(r => r.Eliminar(aviso), Times.Once);
    }

    [Fact]
    public async Task Eliminar_Inexistente_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.EliminarAsync(Guid.NewGuid()));
    }
}
