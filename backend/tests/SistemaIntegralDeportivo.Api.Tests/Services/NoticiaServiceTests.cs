using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Noticias del club (TDD). Reglas: no se publica algo que ya nació vencido, la vista
/// del alumno (soloVigentes) oculta las vencidas y las apagadas, y editar corrige el
/// contenido sin revivir una noticia que el profe bajó. El orden por importancia lo
/// resuelve el repositorio (es una consulta), no este service.
/// </summary>
public class NoticiaServiceTests
{
    private readonly Mock<INoticiaRepository> _repo;
    private readonly NoticiaService _service;

    public NoticiaServiceTests()
    {
        _repo = new Mock<INoticiaRepository>();
        _service = new NoticiaService(_repo.Object);
    }

    private static GuardarNoticiaDto Dto(DateOnly? vence = null, bool importante = false) => new()
    {
        Titulo = "Sin clases el viernes",
        Mensaje = "Se suspende por el torneo interno.",
        Importante = importante,
        VenceEl = vence,
    };

    private static Noticia Noticia(bool activo = true, DateOnly? vence = null, bool importante = false) => new()
    {
        Titulo = "x", Mensaje = "y", Activo = activo, VenceEl = vence, Importante = importante,
    };

    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Crear_CasoFeliz_CreaLaNoticiaActiva()
    {
        Noticia? creada = null;
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Noticia>(), It.IsAny<CancellationToken>()))
             .Callback((Noticia n, CancellationToken _) => creada = n).Returns(Task.CompletedTask);

        var res = await _service.CrearAsync(Dto(Hoy.AddDays(3)));

        Assert.NotNull(creada);
        Assert.True(creada!.Activo);
        Assert.Equal("Sin clases el viernes", creada.Titulo);
        Assert.True(res.Activo);
    }

    [Fact]
    public async Task Crear_SinVencimiento_EsValido()
    {
        var res = await _service.CrearAsync(Dto(vence: null));

        Assert.Null(res.VenceEl);
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Noticia>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crear_VencimientoPasado_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(Dto(Hoy.AddDays(-1))));

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Noticia>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Crear_Importante_QuedaMarcada()
    {
        Noticia? creada = null;
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Noticia>(), It.IsAny<CancellationToken>()))
             .Callback((Noticia n, CancellationToken _) => creada = n).Returns(Task.CompletedTask);

        var res = await _service.CrearAsync(Dto(importante: true));

        Assert.True(creada!.Importante);
        Assert.True(res.Importante);
    }

    // ── Editar: el que la publicó la corrige ──

    [Fact]
    public async Task Editar_CambiaContenidoImportanciaYVencimiento()
    {
        var noticia = Noticia(vence: Hoy.AddDays(1));
        _repo.Setup(r => r.ObtenerAsync(noticia.Id, It.IsAny<CancellationToken>())).ReturnsAsync(noticia);

        var res = await _service.EditarAsync(noticia.Id, new GuardarNoticiaDto
        {
            Titulo = "  Cambió el horario  ",
            Mensaje = "  Arrancamos 19:00.  ",
            Importante = true,
            VenceEl = Hoy.AddDays(5),
        });

        Assert.Equal("Cambió el horario", noticia.Titulo); // recortado
        Assert.Equal("Arrancamos 19:00.", noticia.Mensaje);
        Assert.True(noticia.Importante);
        Assert.Equal(Hoy.AddDays(5), noticia.VenceEl);
        Assert.Equal("Cambió el horario", res.Titulo);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Editar_NoRevivaUnaNoticiaApagada()
    {
        // Prender y apagar tiene su propio endpoint: corregir un título no tiene que
        // volver a publicarle a todo el club algo que el profe bajó.
        var noticia = Noticia(activo: false);
        _repo.Setup(r => r.ObtenerAsync(noticia.Id, It.IsAny<CancellationToken>())).ReturnsAsync(noticia);

        await _service.EditarAsync(noticia.Id, Dto());

        Assert.False(noticia.Activo);
    }

    [Fact]
    public async Task Editar_VencimientoPasado_Lanza()
    {
        var noticia = Noticia();
        _repo.Setup(r => r.ObtenerAsync(noticia.Id, It.IsAny<CancellationToken>())).ReturnsAsync(noticia);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.EditarAsync(noticia.Id, Dto(Hoy.AddDays(-1))));

        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Editar_Inexistente_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.EditarAsync(Guid.NewGuid(), Dto()));
    }

    // ── Qué ve el alumno ──

    [Fact]
    public async Task Listar_SoloVigentes_OcultaLasVencidas()
    {
        // El repo (soloActivas) devuelve activas; el service filtra por vencimiento.
        var vigente = Noticia(vence: Hoy.AddDays(2));
        var sinVence = Noticia(vence: null);
        var vencida = Noticia(vence: Hoy.AddDays(-1));
        _repo.Setup(r => r.ListarAsync(true, It.IsAny<CancellationToken>()))
             .ReturnsAsync([vigente, sinVence, vencida]);

        var res = await _service.ListarAsync(soloVigentes: true);

        Assert.Equal(2, res.Count);
        Assert.DoesNotContain(res, n => n.Id == vencida.Id);
    }

    [Fact]
    public async Task Listar_VenceHoy_SigueVigente()
    {
        _repo.Setup(r => r.ListarAsync(true, It.IsAny<CancellationToken>()))
             .ReturnsAsync([Noticia(vence: Hoy)]);

        var res = await _service.ListarAsync(soloVigentes: true);

        Assert.Single(res); // vence al final del día de hoy: todavía se ve
    }

    [Fact]
    public async Task Listar_ConservaElOrdenDelRepositorio()
    {
        // Las destacadas van primero (lo ordena la consulta): el filtro por vencimiento
        // no puede reacomodarlas, porque ese orden es el que usa el Inicio del portal.
        var destacada = Noticia(importante: true);
        var comun = Noticia();
        _repo.Setup(r => r.ListarAsync(true, It.IsAny<CancellationToken>()))
             .ReturnsAsync([destacada, comun]);

        var res = await _service.ListarAsync(soloVigentes: true);

        Assert.Equal(destacada.Id, res[0].Id);
        Assert.True(res[0].Importante);
    }

    [Fact]
    public async Task Listar_ParaProfe_TraeTodasSinFiltrarVencimiento()
    {
        // El profe (soloVigentes=false) ve todo para poder gestionarlo.
        _repo.Setup(r => r.ListarAsync(false, It.IsAny<CancellationToken>()))
             .ReturnsAsync([Noticia(vence: Hoy.AddDays(-5)), Noticia(activo: false)]);

        var res = await _service.ListarAsync(soloVigentes: false);

        Assert.Equal(2, res.Count);
    }

    // ── Prender / apagar / borrar ──

    [Fact]
    public async Task CambiarActivo_ApagaLaNoticia()
    {
        var noticia = Noticia(activo: true);
        _repo.Setup(r => r.ObtenerAsync(noticia.Id, It.IsAny<CancellationToken>())).ReturnsAsync(noticia);

        await _service.CambiarActivoAsync(noticia.Id, false);

        Assert.False(noticia.Activo);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Eliminar_BorraLaNoticia()
    {
        var noticia = Noticia();
        _repo.Setup(r => r.ObtenerAsync(noticia.Id, It.IsAny<CancellationToken>())).ReturnsAsync(noticia);

        await _service.EliminarAsync(noticia.Id);

        _repo.Verify(r => r.Eliminar(noticia), Times.Once);
    }

    [Fact]
    public async Task Eliminar_Inexistente_Lanza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.EliminarAsync(Guid.NewGuid()));
    }
}
