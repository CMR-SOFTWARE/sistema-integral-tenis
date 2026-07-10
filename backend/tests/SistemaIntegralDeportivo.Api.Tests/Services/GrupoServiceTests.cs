using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Reglas de membresías de grupos (TDD, ADR-0005): cupo máximo, no duplicar
/// miembro activo, solo alumnos Activos, baja con historia y reactivación.
/// </summary>
public class GrupoServiceTests
{
    private readonly Mock<IGrupoRepository> _grupos;
    private readonly Mock<IAlumnoRepository> _alumnos;
    private readonly Mock<ICargoRepository> _cargos;
    private readonly GrupoService _service;

    private static readonly Guid GrupoId = Guid.NewGuid();
    private static readonly Guid AlumnoId = Guid.NewGuid();

    public GrupoServiceTests()
    {
        _grupos = new Mock<IGrupoRepository>();
        _alumnos = new Mock<IAlumnoRepository>();
        _cargos = new Mock<ICargoRepository>();
        _service = new GrupoService(_grupos.Object, _alumnos.Object, _cargos.Object);

        // Por defecto: nadie debe nada
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);

        // Escenario base feliz: grupo con cupo 4 y 2 miembros, alumno activo sin membresía
        _grupos.Setup(g => g.ObtenerAsync(GrupoId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(GrupoCon(cupo: 4));
        _grupos.Setup(g => g.ContarMiembrosActivosAsync(GrupoId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(2);
        _grupos.Setup(g => g.ObtenerMembresiaAsync(GrupoId, AlumnoId, It.IsAny<CancellationToken>()))
               .ReturnsAsync((AlumnoGrupo?)null);
        _alumnos.Setup(a => a.ObtenerAsync(AlumnoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(AlumnoActivo());
    }

    private static Grupo GrupoCon(int? cupo) => new()
    {
        Id = GrupoId,
        Nombre = "Intermedios martes",
        CupoMaximo = cupo,
    };

    private static Alumno AlumnoActivo() => new()
    {
        Id = AlumnoId,
        Nombre = "Juan",
        Apellido = "Pérez",
        Dni = "30111222",
        Telefono = "+549115555",
        Estado = EstadoAlumno.Activo,
    };

    // ─────────────────────────────────────────────
    // Asignar: reglas de cupo, duplicado y estado
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Asignar_GrupoLleno_LanzaYNoPersiste()
    {
        _grupos.Setup(g => g.ContarMiembrosActivosAsync(GrupoId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(4); // cupo 4, ya hay 4

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AsignarAlumnoAsync(GrupoId, AlumnoId));

        _grupos.Verify(g => g.AgregarMembresiaAsync(It.IsAny<AlumnoGrupo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Asignar_GrupoSinCupoMaximo_NoLimita()
    {
        _grupos.Setup(g => g.ObtenerAsync(GrupoId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(GrupoCon(cupo: null)); // sin límite
        _grupos.Setup(g => g.ContarMiembrosActivosAsync(GrupoId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(99);

        await _service.AsignarAlumnoAsync(GrupoId, AlumnoId);

        _grupos.Verify(g => g.AgregarMembresiaAsync(
            It.Is<AlumnoGrupo>(m => m.AlumnoId == AlumnoId && m.GrupoId == GrupoId && m.FechaBaja == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Asignar_MiembroYaActivo_Lanza()
    {
        _grupos.Setup(g => g.ObtenerMembresiaAsync(GrupoId, AlumnoId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new AlumnoGrupo { GrupoId = GrupoId, AlumnoId = AlumnoId, FechaBaja = null });

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AsignarAlumnoAsync(GrupoId, AlumnoId));
    }

    [Fact]
    public async Task Asignar_AlumnoNoActivo_Lanza()
    {
        var suspendido = AlumnoActivo();
        suspendido.Estado = EstadoAlumno.Suspendido;
        _alumnos.Setup(a => a.ObtenerAsync(AlumnoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(suspendido);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AsignarAlumnoAsync(GrupoId, AlumnoId));
    }

    [Fact]
    public async Task Asignar_ConCuotaVencida_Lanza()
    {
        // Debe una clase de hace 2 meses: no puede sumarse a clases nuevas
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([new Cargo
               {
                   AlumnoId = AlumnoId, Tipo = TipoCargo.Clase, Concepto = "x", Monto = 4_000m,
                   Fecha = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2),
               }]);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.AsignarAlumnoAsync(GrupoId, AlumnoId));

        _grupos.Verify(g => g.AgregarMembresiaAsync(It.IsAny<AlumnoGrupo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Asignar_CasoFeliz_CreaMembresiaActiva()
    {
        await _service.AsignarAlumnoAsync(GrupoId, AlumnoId);

        _grupos.Verify(g => g.AgregarMembresiaAsync(
            It.Is<AlumnoGrupo>(m => m.AlumnoId == AlumnoId && m.FechaBaja == null),
            It.IsAny<CancellationToken>()), Times.Once);
        _grupos.Verify(g => g.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────
    // Historia: reactivación tras baja y quitar sin borrar
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Asignar_ConBajaPrevia_ReactivaLaMembresia()
    {
        var membresiaVieja = new AlumnoGrupo
        {
            GrupoId = GrupoId,
            AlumnoId = AlumnoId,
            FechaAlta = DateTime.UtcNow.AddMonths(-6),
            FechaBaja = DateTime.UtcNow.AddMonths(-3), // se fue hace 3 meses
        };
        _grupos.Setup(g => g.ObtenerMembresiaAsync(GrupoId, AlumnoId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(membresiaVieja);

        await _service.AsignarAlumnoAsync(GrupoId, AlumnoId);

        Assert.Null(membresiaVieja.FechaBaja); // reactivada
        _grupos.Verify(g => g.AgregarMembresiaAsync(It.IsAny<AlumnoGrupo>(), It.IsAny<CancellationToken>()), Times.Never); // no duplica fila
        _grupos.Verify(g => g.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Quitar_MiembroActivo_SeteaFechaBaja()
    {
        var membresia = new AlumnoGrupo { GrupoId = GrupoId, AlumnoId = AlumnoId, FechaBaja = null };
        _grupos.Setup(g => g.ObtenerMembresiaAsync(GrupoId, AlumnoId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(membresia);

        await _service.QuitarAlumnoAsync(GrupoId, AlumnoId);

        Assert.NotNull(membresia.FechaBaja); // baja lógica con historia
        _grupos.Verify(g => g.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Quitar_SinMembresiaActiva_Lanza()
    {
        _grupos.Setup(g => g.ObtenerMembresiaAsync(GrupoId, AlumnoId, It.IsAny<CancellationToken>()))
               .ReturnsAsync((AlumnoGrupo?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.QuitarAlumnoAsync(GrupoId, AlumnoId));
    }
}
