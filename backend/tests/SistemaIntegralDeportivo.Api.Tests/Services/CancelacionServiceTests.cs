using Moq;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// El merge de la vista Cancelaciones: turnos enteros + avisos de alumnos
/// en una sola lista cronológica con el chip por-quién correcto.
/// </summary>
public class CancelacionServiceTests
{
    private readonly Mock<ITurnoRepository> _turnos = new();
    private readonly CancelacionService _service;

    public CancelacionServiceTests()
    {
        _service = new CancelacionService(_turnos.Object);
    }

    [Fact]
    public async Task ListarRecientes_MezclaAmbasFuentesOrdenadasPorFecha_YMapeaPorQuien()
    {
        var ahora = DateTime.UtcNow;

        // Turno entero cancelado por el profe AYER
        var turnoCancelado = new Turno
        {
            Fecha = new DateOnly(2026, 7, 14),
            HoraInicio = new TimeOnly(18, 0),
            Estado = EstadoTurno.Cancelado,
            CanceladoMotivo = "Lluvia",
            CanceladoEl = ahora.AddDays(-1),
            CanceladoPor = CanceladoPor.Profesor,
            Horario = new Horario
            {
                CanchaId = Guid.NewGuid(), Dia = DayOfWeek.Tuesday,
                HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60,
                Grupo = new Grupo { Nombre = "Intermedios" },
            },
        };
        _turnos.Setup(t => t.ListarCanceladosRecientesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([turnoCancelado]);

        // Aviso de un alumno HOY (más reciente → va primero)
        var turnoVigente = new Turno
        {
            Fecha = new DateOnly(2026, 7, 15),
            HoraInicio = new TimeOnly(9, 0),
            Horario = new Horario
            {
                CanchaId = Guid.NewGuid(), Dia = DayOfWeek.Wednesday,
                HoraInicio = new TimeOnly(9, 0), DuracionMinutos = 60,
                Grupo = new Grupo { Nombre = "Avanzados" },
            },
        };
        var aviso = new TurnoParticipante
        {
            Turno = turnoVigente,
            CanceloEl = ahora,
            CancelacionMotivo = "Viaje",
            Alumno = new Alumno
            {
                Nombre = "Juan", Apellido = "Pérez",
                Dni = "1", Telefono = "+549111111",
            },
        };
        _turnos.Setup(t => t.ListarAvisosRecientesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([aviso]);

        var lista = await _service.ListarRecientesAsync(5);

        Assert.Equal(2, lista.Count);
        // El aviso del alumno es más reciente → primero
        Assert.Equal("Alumno", lista[0].Por);
        Assert.Equal("Juan Pérez", lista[0].AlumnoNombre);
        Assert.Equal("+549111111", lista[0].Telefono);
        Assert.Equal("Avanzados", lista[0].Titulo);
        Assert.Equal("Profesor", lista[1].Por);
        Assert.Equal("Intermedios", lista[1].Titulo);
        Assert.Null(lista[1].AlumnoNombre); // grupal: no hay UN afectado
    }

    [Fact]
    public async Task ListarRecientes_RespetaElTope()
    {
        Turno Cancelado(int haceDias) => new()
        {
            Fecha = new DateOnly(2026, 7, 14),
            HoraInicio = new TimeOnly(18, 0),
            Estado = EstadoTurno.Cancelado,
            CanceladoEl = DateTime.UtcNow.AddDays(-haceDias),
            Horario = new Horario
            {
                CanchaId = Guid.NewGuid(), Dia = DayOfWeek.Tuesday,
                HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60,
                Grupo = new Grupo { Nombre = "Intermedios" },
            },
        };
        _turnos.Setup(t => t.ListarCanceladosRecientesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([Cancelado(1), Cancelado(2)]);
        _turnos.Setup(t => t.ListarAvisosRecientesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);

        var lista = await _service.ListarRecientesAsync(2);

        Assert.Equal(2, lista.Count);
    }
}
