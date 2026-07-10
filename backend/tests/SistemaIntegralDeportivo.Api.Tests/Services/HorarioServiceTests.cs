using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Reglas de horarios (TDD): solapamiento POR CANCHA y grupal XOR individual.
/// </summary>
public class HorarioServiceTests
{
    private static readonly Guid Cancha1 = Guid.NewGuid();
    private static readonly Guid Cancha2 = Guid.NewGuid();
    private static readonly Guid GrupoId = Guid.NewGuid();
    private static readonly Guid AlumnoId = Guid.NewGuid();

    private readonly Mock<IHorarioRepository> _repo;
    private readonly HorarioService _service;

    public HorarioServiceTests()
    {
        _repo = new Mock<IHorarioRepository>();
        _service = new HorarioService(_repo.Object);

        // Ya existe: martes 18:00-19:00 en Cancha 1
        var existente = new Horario
        {
            CanchaId = Cancha1,
            GrupoId = GrupoId,
            Dia = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(18, 0),
            DuracionMinutos = 60,
        };
        _repo.Setup(r => r.ListarPorCanchaYDiaAsync(Cancha1, DayOfWeek.Tuesday, It.IsAny<CancellationToken>()))
             .ReturnsAsync([existente]);
        _repo.Setup(r => r.ListarPorCanchaYDiaAsync(Cancha2, It.IsAny<DayOfWeek>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Horario h, CancellationToken _) => h);
    }

    private static CreateHorarioDto Dto(Guid cancha, TimeOnly hora, int duracion = 60) => new()
    {
        CanchaId = cancha,
        GrupoId = GrupoId,
        Dia = DayOfWeek.Tuesday,
        HoraInicio = hora,
        DuracionMinutos = duracion,
    };

    [Fact]
    public async Task Crear_SolapaEnLaMismaCancha_Lanza()
    {
        // 18:30-19:30 pisa al de 18:00-19:00 en la misma cancha
        var dto = Dto(Cancha1, new TimeOnly(18, 30));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Crear_MismaHoraEnOtraCancha_Crea()
    {
        // El profe tiene staff: dos clases a la vez en canchas distintas, OK
        var dto = Dto(Cancha2, new TimeOnly(18, 0));

        await _service.CrearAsync(dto);

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crear_ContiguoEnLaMismaCancha_NoEsSolapamiento()
    {
        // 19:00-20:00 arranca justo cuando termina el de 18:00 → válido
        var dto = Dto(Cancha1, new TimeOnly(19, 0));

        await _service.CrearAsync(dto);

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Horario>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crear_SinGrupoNiAlumno_Lanza()
    {
        var dto = Dto(Cancha2, new TimeOnly(10, 0));
        dto.GrupoId = null; // ni grupo ni alumno

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
    }

    [Fact]
    public async Task Crear_ConGrupoYAlumnoALaVez_Lanza()
    {
        var dto = Dto(Cancha2, new TimeOnly(10, 0));
        dto.AlumnoId = AlumnoId; // grupo Y alumno: ambiguo

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
    }
}
