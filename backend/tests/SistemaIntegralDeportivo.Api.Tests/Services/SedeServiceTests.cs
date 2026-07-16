using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Reglas de sedes (TDD): la baja es LÓGICA (nunca DELETE físico) y no se
/// puede desactivar una sede que todavía tiene horarios activos.
/// </summary>
public class SedeServiceTests
{
    private readonly Mock<ISedeRepository> _sedes;
    private readonly Mock<IHorarioRepository> _horarios;
    private readonly SedeService _service;

    public SedeServiceTests()
    {
        _sedes = new Mock<ISedeRepository>();
        _horarios = new Mock<IHorarioRepository>();
        _service = new SedeService(_sedes.Object, _horarios.Object);

        // Por defecto: sin horarios activos en ninguna cancha
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);
    }

    /// <summary>Sede con una cancha, registrada en el mock del repo.</summary>
    private Sede SedeConCancha(bool activo = true)
    {
        var sede = new Sede { Nombre = "Club Norte", Activo = activo };
        var cancha = new Cancha { SedeId = sede.Id, Nombre = "Cancha 1" };
        sede.Canchas.Add(cancha);
        _sedes.Setup(s => s.ObtenerAsync(sede.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(sede);
        return sede;
    }

    [Fact]
    public async Task Desactivar_SedeSinHorarios_LaMarcaInactiva_SinBorrar()
    {
        var sede = SedeConCancha();

        await _service.DesactivarAsync(sede.Id);

        Assert.False(sede.Activo); // baja lógica: la sede sigue existiendo
        _sedes.Verify(s => s.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Desactivar_SedeConHorariosActivos_Lanza()
    {
        // No se desactiva una sede en uso: primero hay que bajar sus horarios
        var sede = SedeConCancha();
        var canchaId = sede.Canchas.First().Id;
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([new Horario
                 {
                     CanchaId = canchaId,
                     Dia = DayOfWeek.Tuesday,
                     HoraInicio = new TimeOnly(18, 0),
                     DuracionMinutos = 60,
                 }]);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.DesactivarAsync(sede.Id));

        Assert.True(sede.Activo);
        _sedes.Verify(s => s.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Desactivar_HorarioActivoEnOTRASede_NoBloquea()
    {
        var sede = SedeConCancha();
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([new Horario
                 {
                     CanchaId = Guid.NewGuid(), // cancha de otra sede
                     Dia = DayOfWeek.Tuesday,
                     HoraInicio = new TimeOnly(18, 0),
                     DuracionMinutos = 60,
                 }]);

        await _service.DesactivarAsync(sede.Id);

        Assert.False(sede.Activo);
    }

    [Fact]
    public async Task Desactivar_Inexistente_Lanza()
    {
        _sedes.Setup(s => s.ObtenerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((Sede?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.DesactivarAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Reactivar_SedeInactiva_LaVuelveActiva()
    {
        var sede = SedeConCancha(activo: false);

        await _service.ReactivarAsync(sede.Id);

        Assert.True(sede.Activo);
        _sedes.Verify(s => s.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
