using Moq;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// La política de cuota MENSUAL manual (la costura volátil de la facturación):
/// un cargo Cuota por alumno activo = su arancel, idempotente, sin arancel no
/// cobra, y solo el mes corriente (no factura retroactivo). Se usa el mes de
/// "hoy" para que el test no dependa de la fecha en que corre.
/// </summary>
public class CuotaMensualManualTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly Mock<IAlumnoRepository> _alumnos = new();
    private readonly Mock<ICargoRepository> _cargos = new();
    private readonly CuotaMensualManual _politica;
    private readonly List<Cargo> _agregados = [];
    private readonly List<Cargo> _delMes = [];

    public CuotaMensualManualTests()
    {
        _politica = new CuotaMensualManual(_alumnos.Object, _cargos.Object);
        _cargos.Setup(c => c.ListarDelMesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(() => [.. _delMes]);
        _cargos.Setup(c => c.AgregarAsync(It.IsAny<Cargo>(), It.IsAny<CancellationToken>()))
               .Callback((Cargo c, CancellationToken _) => _agregados.Add(c))
               .Returns(Task.CompletedTask);
        // Default de seguridad; HayActivos lo sobrescribe con los ids que tomen clase
        _alumnos.Setup(a => a.ListarConClaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
    }

    private static Alumno Activo(decimal? arancel) => new()
    {
        Nombre = "A", Apellido = "B", Telefono = "1", Estado = EstadoAlumno.Activo, Arancel = arancel,
    };

    private void HayActivos(params Alumno[] alumnos)
    {
        _alumnos.Setup(a => a.ListarAsync(null, EstadoAlumno.Activo, It.IsAny<CancellationToken>()))
                .ReturnsAsync(alumnos);
        // Por defecto todos toman clases (los tests de "sin clase" lo overridean)
        _alumnos.Setup(a => a.ListarConClaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(alumnos.Select(x => x.Id).ToHashSet());
    }

    [Fact]
    public async Task Genera_UnCargoCuota_PorAlumnoActivo_ConSuArancel()
    {
        HayActivos(Activo(30_000m), Activo(25_000m));

        await _politica.GenerarCargosDelMesAsync(Hoy.Year, Hoy.Month);

        Assert.Equal(2, _agregados.Count);
        Assert.All(_agregados, c => Assert.Equal(TipoCargo.Cuota, c.Tipo));
        Assert.Contains(_agregados, c => c.Monto == 30_000m);
        Assert.Contains(_agregados, c => c.Monto == 25_000m);
    }

    [Fact]
    public async Task Idempotente_NoDuplica_SiElAlumnoYaTieneCuotaDelMes()
    {
        var alumno = Activo(30_000m);
        HayActivos(alumno);
        _delMes.Add(new Cargo
        {
            AlumnoId = alumno.Id, Tipo = TipoCargo.Cuota, Concepto = "Cuota",
            Monto = 30_000m, Fecha = new DateOnly(Hoy.Year, Hoy.Month, 1),
        });

        await _politica.GenerarCargosDelMesAsync(Hoy.Year, Hoy.Month);

        Assert.Empty(_agregados); // ya la tenía
    }

    [Fact]
    public async Task AlumnoSinArancel_NoSeCobra()
    {
        HayActivos(Activo(null), Activo(0m));

        await _politica.GenerarCargosDelMesAsync(Hoy.Year, Hoy.Month);

        Assert.Empty(_agregados);
    }

    [Fact]
    public async Task AlumnoSinClase_NoSeCobra_AunqueTengaArancel()
    {
        var alumno = Activo(30_000m);
        _alumnos.Setup(a => a.ListarAsync(null, EstadoAlumno.Activo, It.IsAny<CancellationToken>()))
                .ReturnsAsync([alumno]);
        // No toma clases: no está en ningún grupo ni tiene horario individual
        _alumnos.Setup(a => a.ListarConClaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<Guid>());

        await _politica.GenerarCargosDelMesAsync(Hoy.Year, Hoy.Month);

        Assert.Empty(_agregados);
    }

    [Fact]
    public async Task MesPasado_NoFacturaRetroactivo()
    {
        HayActivos(Activo(30_000m));
        var pasado = Hoy.AddMonths(-2);

        await _politica.GenerarCargosDelMesAsync(pasado.Year, pasado.Month);

        Assert.Empty(_agregados);
    }
}
