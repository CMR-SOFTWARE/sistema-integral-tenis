using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Raquetas del alumno (M3, TDD). Dos reglas: la PERTENENCIA —solo se toca una
/// raqueta del alumno indicado, y eso vale también para su historial— y cuál es
/// el ÚLTIMO encordado, que es el dato que el profe mira en la ficha.
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

        var dto = new GuardarRaquetaDto { Marca = "Babolat", Modelo = "Pure Aero" };
        var res = await _service.AgregarAsync(Yo, dto);

        Assert.NotNull(creada);
        Assert.Equal(Yo, creada!.AlumnoId);
        Assert.Equal("Babolat", creada.Marca);
        Assert.Equal("Pure Aero", creada.Modelo);
        Assert.Equal("Babolat", res.Marca);
        Assert.Null(res.UltimoEncordado); // recién creada: todavía sin encordar
    }

    [Fact]
    public async Task Editar_RaquetaMia_ActualizaLosDatos()
    {
        var raqueta = RaquetaDe(Yo);

        await _service.EditarAsync(Yo, raqueta.Id, new GuardarRaquetaDto { Marca = "Head", Modelo = "Speed MP" });

        Assert.Equal("Head", raqueta.Marca);
        Assert.Equal("Speed MP", raqueta.Modelo);
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

    // ─────────────────────────────────────────────
    // El historial de encordados
    // ─────────────────────────────────────────────

    private static GuardarEncordadoDto Encordar(string cuerda, DateOnly fecha) => new()
    {
        CuerdaVertical = cuerda, TensionVertical = "24 kg", Fecha = fecha,
    };

    /// <summary>Agrega un encordado YA GUARDADO a la raqueta (para armar historiales).</summary>
    private static Encordado Historico(Raqueta r, string cuerda, DateOnly fecha, DateTime? creadoEl = null)
    {
        var e = new Encordado
        {
            RaquetaId = r.Id, CuerdaVertical = cuerda, Fecha = fecha,
            CreadoEl = creadoEl ?? DateTime.UtcNow,
        };
        r.Encordados.Add(e);
        return e;
    }

    [Fact]
    public async Task AgregarEncordado_EnRaquetaMia_LoSumaAlHistorial()
    {
        var raqueta = RaquetaDe(Yo);
        var fecha = new DateOnly(2026, 8, 1);

        var res = await _service.AgregarEncordadoAsync(Yo, raqueta.Id, Encordar("Luxilon ALU Power", fecha));

        _repo.Verify(x => x.AgregarEncordadoAsync(
            It.Is<Encordado>(e => e.RaquetaId == raqueta.Id && e.CuerdaVertical == "Luxilon ALU Power"),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("Luxilon ALU Power", res.UltimoEncordado!.CuerdaVertical);
        Assert.Equal(fecha, res.UltimoEncordado.Fecha);
    }

    [Fact]
    public async Task AgregarEncordado_EnRaquetaDeOtro_Lanza()
    {
        var ajena = RaquetaDe(Otro);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AgregarEncordadoAsync(Yo, ajena.Id, Encordar("X", new DateOnly(2026, 8, 1))));

        _repo.Verify(x => x.AgregarEncordadoAsync(
            It.IsAny<Encordado>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// El último es el de FECHA más nueva, no el último cargado: el alumno puede
    /// cargar hoy un encordado que le hicieron el mes pasado.
    /// </summary>
    [Fact]
    public async Task Mis_UltimoEncordado_EsElDeFechaMasNueva()
    {
        var raqueta = RaquetaDe(Yo);
        Historico(raqueta, "El nuevo", new DateOnly(2026, 7, 1), creadoEl: DateTime.UtcNow.AddDays(-10));
        Historico(raqueta, "El viejo", new DateOnly(2026, 3, 1), creadoEl: DateTime.UtcNow); // cargado después
        _repo.Setup(x => x.ListarPorAlumnoAsync(Yo, It.IsAny<CancellationToken>()))
             .ReturnsAsync([raqueta]);

        var res = await _service.MisAsync(Yo);

        var dto = Assert.Single(res);
        Assert.Equal("El nuevo", dto.UltimoEncordado!.CuerdaVertical);
        // Y el historial baja del más nuevo al más viejo
        Assert.Equal(["El nuevo", "El viejo"], dto.Encordados.Select(e => e.CuerdaVertical));
    }

    /// <summary>Dos encordados del mismo día: gana el cargado último (fue la corrección).</summary>
    [Fact]
    public async Task Mis_ConDosEncordadosElMismoDia_GanaElCargadoDespues()
    {
        var raqueta = RaquetaDe(Yo);
        var mismoDia = new DateOnly(2026, 8, 1);
        Historico(raqueta, "Mal cargado", mismoDia, creadoEl: DateTime.UtcNow.AddHours(-2));
        Historico(raqueta, "Corregido", mismoDia, creadoEl: DateTime.UtcNow);
        _repo.Setup(x => x.ListarPorAlumnoAsync(Yo, It.IsAny<CancellationToken>()))
             .ReturnsAsync([raqueta]);

        var res = await _service.MisAsync(Yo);

        Assert.Equal("Corregido", res.Single().UltimoEncordado!.CuerdaVertical);
    }

    [Fact]
    public async Task AgregarEncordado_Hibrido_GuardaLasDosCuerdas()
    {
        var raqueta = RaquetaDe(Yo);
        var dto = Encordar("Luxilon ALU Power", new DateOnly(2026, 8, 1));
        dto.CuerdaHorizontal = "Wilson NXT";
        dto.TensionHorizontal = "26 kg";

        var res = await _service.AgregarEncordadoAsync(Yo, raqueta.Id, dto);

        Assert.True(res.UltimoEncordado!.EsHibrido);
        Assert.Equal("Wilson NXT", res.UltimoEncordado.CuerdaHorizontal);
    }

    [Fact]
    public async Task BorrarEncordado_DeRaquetaDeOtro_Lanza_YNoElimina()
    {
        var ajena = RaquetaDe(Otro);
        var suyo = Historico(ajena, "X", new DateOnly(2026, 8, 1));
        _repo.Setup(x => x.ObtenerEncordadoAsync(suyo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(suyo);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.BorrarEncordadoAsync(Yo, suyo.Id));

        _repo.Verify(x => x.EliminarEncordado(It.IsAny<Encordado>()), Times.Never);
    }

    [Fact]
    public async Task BorrarEncordado_Mio_LoElimina()
    {
        var raqueta = RaquetaDe(Yo);
        var mio = Historico(raqueta, "X", new DateOnly(2026, 8, 1));
        _repo.Setup(x => x.ObtenerEncordadoAsync(mio.Id, It.IsAny<CancellationToken>())).ReturnsAsync(mio);

        await _service.BorrarEncordadoAsync(Yo, mio.Id);

        _repo.Verify(x => x.EliminarEncordado(mio), Times.Once);
    }
}
