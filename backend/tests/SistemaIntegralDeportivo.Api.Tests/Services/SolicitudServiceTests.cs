using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Lista de espera (TDD): unirse a un club crea la ficha, que queda esperando
/// porque todavía no tiene clase (ya no hay un estado EnEspera). El profe ve la
/// lista, que junta a los que no tienen ninguna clase y a los que pidieron sumarse
/// a una y él no resolvió — esos pueden ser alumnos y estar acá a la vez.
/// </summary>
public class SolicitudServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly Mock<IAlumnoService> _alumnos;
    private readonly Mock<IAlumnoRepository> _alumnoRepo;
    private readonly Mock<ISolicitudCupoRepository> _pedidos;
    private readonly Mock<ITenantRepository> _tenants;
    private readonly Mock<ITenantActual> _tenantActual;
    private readonly Mock<IUsuarioActual> _usuario;
    private readonly SolicitudService _service;

    public SolicitudServiceTests()
    {
        _alumnos = new Mock<IAlumnoService>();
        _alumnoRepo = new Mock<IAlumnoRepository>();
        _pedidos = new Mock<ISolicitudCupoRepository>();
        _tenants = new Mock<ITenantRepository>();
        _tenantActual = new Mock<ITenantActual>();
        _usuario = new Mock<IUsuarioActual>(); // por defecto: no es staff (dueño ve toda la espera)
        _service = new SolicitudService(
            _alumnos.Object, _alumnoRepo.Object, _pedidos.Object,
            _tenants.Object, _tenantActual.Object, _usuario.Object);

        // Por defecto: el club existe y está activo, sin ficha previa
        _tenants.Setup(t => t.ObtenerPorIdAsync(TenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClubActivo());
        _alumnoRepo.Setup(a => a.ObtenerPorUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Alumno?)null);
        // Por defecto: nadie tiene clase y no hay pedidos pendientes
        _alumnoRepo.Setup(a => a.ListarConClaseAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync([]);
        _alumnoRepo.Setup(a => a.FiltrarConClaseAsync(
                       It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync([]);
        _pedidos.Setup(p => p.ListarPorEstadoAsync(
                    EstadoSolicitudGrupo.Pendiente, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
    }

    /// <summary>Ficha activa del club, con el mínimo para construir el DTO.</summary>
    private static Alumno Ficha(string nombre, Guid? profe = null) => new()
    {
        Nombre = nombre, Apellido = "Mora", Dni = "40", Telefono = "+1",
        Estado = EstadoAlumno.Activo, ProfesorUserId = profe,
    };

    /// <summary>Las fichas activas que devuelve el repo para el tenant.</summary>
    private void Activas(params Alumno[] fichas) =>
        _alumnoRepo.Setup(a => a.ListarAsync(null, EstadoAlumno.Activo, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(fichas);

    private void ConClase(params Alumno[] fichas) =>
        _alumnoRepo.Setup(a => a.ListarConClaseAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync([.. fichas.Select(f => f.Id)]);

    private static Tenant ClubActivo() => new()
    {
        Id = TenantId, Subdominio = "club-x", Nombre = "Club X",
        Estado = EstadoTenant.Activo,
    };

    /// <summary>Jugador con datos completos (registro segmentado C1).</summary>
    private static Usuario Jugador() => new()
    {
        Id = UserId,
        UserName = "lucas@mail.com",
        Email = "lucas@mail.com",
        Nombre = "Lucas",
        Apellido = "Calderón",
        Dni = "30111222",
        PhoneNumber = "+549115555",
        FechaNacimiento = DateTime.UtcNow.AddYears(-30),
        Categoria = CategoriaAlumno.Cuarta,
    };

    // ── Unirse (crea la ficha directo en la lista de espera) ──

    [Fact]
    public async Task Crear_ClubInexistente_Lanza()
    {
        _tenants.Setup(t => t.ObtenerPorIdAsync(TenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tenant?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(Jugador(), new CrearSolicitudDto { TenantId = TenantId }));
    }

    [Fact]
    public async Task Crear_ClubNoActivo_Lanza()
    {
        var club = ClubActivo();
        club.Estado = EstadoTenant.PendientePago;
        _tenants.Setup(t => t.ObtenerPorIdAsync(TenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(club);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(Jugador(), new CrearSolicitudDto { TenantId = TenantId }));
    }

    [Fact]
    public async Task Crear_YaEstoyEnUnClub_Lanza()
    {
        // Un club por persona POR AHORA (multi-club llega con la reserva de turnos)
        _alumnoRepo.Setup(a => a.ObtenerPorUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Alumno
                   {
                       Nombre = "L", Apellido = "C", Dni = "1", Telefono = "+5",
                       UserId = UserId,
                   });

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(Jugador(), new CrearSolicitudDto { TenantId = TenantId }));
        _alumnos.Verify(a => a.CrearVinculadoAsync(
            It.IsAny<CreateAlumnoDto>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Crear_SinDatosCompletos_Lanza()
    {
        // Cuentas viejas sin fecha de nacimiento: pedirle que complete el perfil
        var usuario = Jugador();
        usuario.FechaNacimiento = null;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAsync(usuario, new CrearSolicitudDto { TenantId = TenantId }));
    }

    [Fact]
    public async Task Crear_OK_CreaLaFichaVinculadaEnEseClubConLosDatos()
    {
        CreateAlumnoDto? dtoUsado = null;
        _alumnos.Setup(a => a.CrearVinculadoAsync(It.IsAny<CreateAlumnoDto>(), UserId, It.IsAny<CancellationToken>()))
                .Callback((CreateAlumnoDto d, Guid _, CancellationToken _) => dtoUsado = d)
                .ReturnsAsync(new AlumnoResponseDto { Id = Guid.NewGuid(), Nombre = "Lucas" });

        await _service.CrearAsync(Jugador(), new CrearSolicitudDto
        {
            TenantId = TenantId, Mensaje = "Juego los martes",
        });

        // Fija el club elegido y crea la ficha con los datos del registro
        _tenantActual.Verify(t => t.Establecer(TenantId), Times.Once);
        Assert.NotNull(dtoUsado);
        Assert.Equal("Lucas", dtoUsado!.Nombre);
        Assert.Equal("30111222", dtoUsado.Dni);
        Assert.Equal("lucas@mail.com", dtoUsado.Email);
        Assert.Equal(CategoriaAlumno.Cuarta, dtoUsado.Categoria);
        Assert.True(dtoUsado.ConsentimientoDatos);       // lo dio al registrarse
        Assert.Equal("Juego los martes", dtoUsado.Notas); // el mensaje queda como nota
    }

    // ── La lista de espera: derivada, no un estado ──

    [Fact]
    public async Task Pendientes_ActivoSinClase_Aparece()
    {
        var ana = Ficha("Ana");
        ana.Notas = "Los jueves";
        Activas(ana);

        var item = Assert.Single(await _service.PendientesAsync());

        Assert.Equal("Ana", item.Nombre);
        Assert.Equal("Los jueves", item.Mensaje);
        Assert.Equal(nameof(MotivoEspera.SinClase), item.Motivo);
        Assert.Null(item.SolicitudId);
    }

    [Fact]
    public async Task Pendientes_ConClaseYSinPedido_NoAparece()
    {
        // Es un alumno de verdad: no está esperando nada.
        var ana = Ficha("Ana");
        Activas(ana);
        ConClase(ana);

        Assert.Empty(await _service.PendientesAsync());
    }

    [Fact]
    public async Task Pendientes_ConClaseYPedidoPendiente_ApareceIgual()
    {
        // El caso que pidió el profe: ya es alumno (viene los lunes) y además está
        // esperando lugar en otra clase. Tiene que estar en las DOS listas.
        var ana = Ficha("Ana");
        Activas(ana);
        ConClase(ana);
        _pedidos.Setup(p => p.ListarPorEstadoAsync(
                    EstadoSolicitudGrupo.Pendiente, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new SolicitudCupo
                    {
                        AlumnoId = ana.Id,
                        Horario = new Horario { Nombre = "Grupo B", CanchaId = Guid.NewGuid() },
                    },
                });

        var item = Assert.Single(await _service.PendientesAsync());

        Assert.Equal("Ana", item.Nombre);
        Assert.Equal(nameof(MotivoEspera.PidioCupo), item.Motivo);
        Assert.Equal("Grupo B", item.Clase);
        Assert.NotNull(item.SolicitudId);
    }

    [Fact]
    public async Task Pendientes_SinClaseYConPedido_ApareceUnaSolaVez()
    {
        // No se duplica: gana el pedido, que es la fila con algo para hacer.
        var ana = Ficha("Ana");
        Activas(ana);
        _pedidos.Setup(p => p.ListarPorEstadoAsync(
                    EstadoSolicitudGrupo.Pendiente, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new SolicitudCupo
                    {
                        AlumnoId = ana.Id,
                        Horario = new Horario { Nombre = "Grupo B", CanchaId = Guid.NewGuid() },
                    },
                });

        var item = Assert.Single(await _service.PendientesAsync());

        Assert.Equal(nameof(MotivoEspera.PidioCupo), item.Motivo);
    }

    [Fact]
    public async Task Pendientes_PausadosYBajas_NoAparecen()
    {
        // El repo devuelve SOLO los activos: el pausado y el de baja no están
        // esperando nada, están afuera del circuito.
        Activas();

        Assert.Empty(await _service.PendientesAsync());
        _alumnoRepo.Verify(
            a => a.ListarAsync(null, EstadoAlumno.Activo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Pendientes_Staff_SoloVeLosSuyos()
    {
        var yo = Guid.NewGuid();
        _usuario.Setup(u => u.EsStaff).Returns(true);
        _usuario.Setup(u => u.UserId).Returns(yo);
        Activas(Ficha("Mío", yo), Ficha("Ajeno", Guid.NewGuid()));

        var item = Assert.Single(await _service.PendientesAsync());

        Assert.Equal("Mío", item.Nombre);
    }

    [Fact]
    public async Task Pendientes_Staff_IgnoraLosPedidosDeOtrosProfes()
    {
        var yo = Guid.NewGuid();
        _usuario.Setup(u => u.EsStaff).Returns(true);
        _usuario.Setup(u => u.UserId).Returns(yo);
        var mio = Ficha("Mío", yo);
        var ajeno = Ficha("Ajeno", Guid.NewGuid());
        Activas(mio, ajeno);
        ConClase(mio, ajeno);
        _pedidos.Setup(p => p.ListarPorEstadoAsync(
                    EstadoSolicitudGrupo.Pendiente, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new SolicitudCupo { AlumnoId = ajeno.Id },
                });

        Assert.Empty(await _service.PendientesAsync());
    }

    [Fact]
    public async Task ContarPendientes_CuentaLosDosMotivos()
    {
        var sinClase = Ficha("Ana");
        var conPedido = Ficha("Beto");
        Activas(sinClase, conPedido);
        ConClase(conPedido);
        _pedidos.Setup(p => p.ListarPorEstadoAsync(
                    EstadoSolicitudGrupo.Pendiente, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { new SolicitudCupo { AlumnoId = conPedido.Id } });

        Assert.Equal(2, await _service.ContarPendientesAsync());
    }

    // ── Quitar de la espera: borra la ficha, así que el guard importa ──

    [Fact]
    public async Task Quitar_Staff_FichaDeOtroProfe_Lanza()
    {
        var yo = Guid.NewGuid();
        _usuario.Setup(u => u.EsStaff).Returns(true);
        _usuario.Setup(u => u.UserId).Returns(yo);
        var ajena = Ficha("Ajeno", Guid.NewGuid());
        _alumnoRepo.Setup(a => a.ObtenerAsync(ajena.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ajena);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.QuitarDeEsperaAsync(ajena.Id));
        _alumnoRepo.Verify(a => a.EliminarDefinitivoAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Quitar_SinClase_BorraLaFicha()
    {
        var ficha = Ficha("Ana");
        _alumnoRepo.Setup(a => a.ObtenerAsync(ficha.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);

        await _service.QuitarDeEsperaAsync(ficha.Id);

        _alumnoRepo.Verify(a => a.EliminarDefinitivoAsync(ficha, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Quitar_ConClase_Lanza()
    {
        // Está en la espera por un pedido, pero es un alumno: esto borraría su ficha
        // entera. Se rechaza el pedido, que no la toca.
        var ficha = Ficha("Ana");
        _alumnoRepo.Setup(a => a.ObtenerAsync(ficha.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);
        _alumnoRepo.Setup(a => a.FiltrarConClaseAsync(
                       It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync([ficha.Id]);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.QuitarDeEsperaAsync(ficha.Id));
        _alumnoRepo.Verify(a => a.EliminarDefinitivoAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
