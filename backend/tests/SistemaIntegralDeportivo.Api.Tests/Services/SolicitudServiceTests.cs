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
    private static readonly Guid DirectorUserId = Guid.NewGuid();

    private readonly Mock<IAlumnoService> _alumnos;
    private readonly Mock<IAlumnoRepository> _alumnoRepo;
    private readonly Mock<ISolicitudCupoRepository> _pedidos;
    private readonly Mock<ITenantRepository> _tenants;
    private readonly Mock<IMembresiaTenantRepository> _membresias;
    private readonly Mock<ITenantActual> _tenantActual;
    private readonly Mock<IUsuarioActual> _usuario;
    private readonly SolicitudService _service;

    public SolicitudServiceTests()
    {
        _alumnos = new Mock<IAlumnoService>();
        _alumnoRepo = new Mock<IAlumnoRepository>();
        _pedidos = new Mock<ISolicitudCupoRepository>();
        _tenants = new Mock<ITenantRepository>();
        _membresias = new Mock<IMembresiaTenantRepository>();
        _tenantActual = new Mock<ITenantActual>();
        _usuario = new Mock<IUsuarioActual>(); // por defecto: no es staff (dueño ve toda la espera)
        _service = new SolicitudService(
            _alumnos.Object, _alumnoRepo.Object, _pedidos.Object,
            _tenants.Object, _membresias.Object, _tenantActual.Object, _usuario.Object);

        // Por defecto: el club existe y está activo, sin ficha previa
        _tenants.Setup(t => t.ObtenerPorIdAsync(TenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClubActivo());
        _tenants.Setup(t => t.ObtenerActualAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClubActivo());
        _alumnoRepo.Setup(a => a.ObtenerPorUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Alumno?)null);
        // Por defecto: nadie tiene clase, no hay pedidos pendientes y no hay empleados
        _alumnoRepo.Setup(a => a.ListarConClaseAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync([]);
        _alumnoRepo.Setup(a => a.FiltrarConClaseAsync(
                       It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync([]);
        _pedidos.Setup(p => p.ListarPorEstadoAsync(
                    EstadoSolicitudGrupo.Pendiente, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
        _membresias.Setup(m => m.ListarConUsuarioAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync([]);
        Activas(); // sin fichas: las listas arrancan vacías
    }

    /// <summary>
    /// La ficha como la devuelve AlumnoService. El mapeo de verdad se prueba en
    /// <c>AlumnoServiceTests</c>; acá solo hace falta que la fila salga con los datos de
    /// esa persona, para verificar QUIÉN entra a la espera y por qué.
    /// </summary>
    private static AlumnoResponseDto Dto(Alumno a) => new()
    {
        Id = a.Id,
        Nombre = a.Nombre,
        Apellido = a.Apellido,
        Dni = a.Dni,
        Telefono = a.Telefono,
        Email = a.Email,
        Notas = a.Notas,
        Categoria = a.Categoria.ToString(),
        Estado = a.Estado.ToString(),
        CreadoEl = a.CreadoEl,
        EnEspera = a.EnEsperaDesde is not null,
    };

    /// <summary>Empleados del club (con su membresía) que devuelve el repo.</summary>
    private void Empleados(params MembresiaTenant[] membresias) =>
        _membresias.Setup(m => m.ListarConUsuarioAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync([.. membresias.Select(m => (m, new Usuario { Nombre = "P", Apellido = "P" }))]);

    /// <summary>Ficha activa del club, con el mínimo para construir el DTO.</summary>
    private static Alumno Ficha(string nombre, Guid? profe = null) => new()
    {
        Nombre = nombre, Apellido = "Mora", Dni = "40", Telefono = "+1",
        Estado = EstadoAlumno.Activo, ProfesorUserId = profe,
    };

    /// <summary>
    /// Las fichas activas que devuelve el repo para el tenant. De paso deja listo el
    /// mapeo: la espera pide las fichas que eligió por motivo y AlumnoService se las
    /// devuelve armadas (el mismo camino que la pestaña Alumnos).
    /// </summary>
    private void Activas(params Alumno[] fichas)
    {
        _alumnoRepo.Setup(a => a.ListarAsync(null, EstadoAlumno.Activo, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(fichas);
        _alumnos.Setup(a => a.ListarPorIdsAsync(
                    It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                    [.. fichas.Where(f => ids.Contains(f.Id)).Select(Dto)]);
    }

    private void ConClase(params Alumno[] fichas) =>
        _alumnoRepo.Setup(a => a.ListarConClaseAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync([.. fichas.Select(f => f.Id)]);

    /// <summary>Ficha que el profe anotó a mano en la espera.</summary>
    private static Alumno Anotada(string nombre, Guid? profe = null)
    {
        var ficha = Ficha(nombre, profe);
        ficha.EnEsperaDesde = DateTime.UtcNow;
        return ficha;
    }

    private static Tenant ClubActivo() => new()
    {
        Id = TenantId, Subdominio = "club-x", Nombre = "Club X",
        Estado = EstadoTenant.Activo, OwnerUserId = DirectorUserId,
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
        Assert.Equal("Los jueves", item.Notas);
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

    // ── El profe que además tiene ficha: trabaja acá, no espera clase ──

    [Fact]
    public async Task Pendientes_ElDirectorSinClase_NoAparece()
    {
        // El director se da de alta para tener su ficha (su categoría, sus raquetas) y
        // no toma clases: sin esto aparecía en la cola como si le faltara horario.
        var director = Ficha("Director");
        director.UserId = DirectorUserId;
        Activas(director);

        Assert.Empty(await _service.PendientesAsync());
    }

    [Fact]
    public async Task Pendientes_ProfeEmpleadoSinClase_NoAparece()
    {
        var profeUserId = Guid.NewGuid();
        var profe = Ficha("Profe");
        profe.UserId = profeUserId;
        Activas(profe);
        Empleados(new MembresiaTenant { UserId = profeUserId, Activo = true });

        Assert.Empty(await _service.PendientesAsync());
    }

    [Fact]
    public async Task Pendientes_ExEmpleadoSinClase_Aparece()
    {
        // Dado de baja como profe, vuelve a ser una persona común: si quiere clase,
        // espera como cualquiera.
        var exProfeUserId = Guid.NewGuid();
        var exProfe = Ficha("ExProfe");
        exProfe.UserId = exProfeUserId;
        Activas(exProfe);
        Empleados(new MembresiaTenant { UserId = exProfeUserId, Activo = false });

        var item = Assert.Single(await _service.PendientesAsync());

        Assert.Equal("ExProfe", item.Nombre);
    }

    [Fact]
    public async Task Pendientes_ElDirectorConPedidoDeCupo_ApareceIgual()
    {
        // El filtro es solo para "no tiene ninguna clase". Si el director pidió lugar en
        // una clase, eso es un pedido de verdad que alguien tiene que resolver.
        var director = Ficha("Director");
        director.UserId = DirectorUserId;
        Activas(director);
        _pedidos.Setup(p => p.ListarPorEstadoAsync(
                    EstadoSolicitudGrupo.Pendiente, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { new SolicitudCupo { AlumnoId = director.Id } });

        var item = Assert.Single(await _service.PendientesAsync());

        Assert.Equal(nameof(MotivoEspera.PidioCupo), item.Motivo);
    }

    [Fact]
    public async Task Pendientes_FichaSinUsuario_Aparece()
    {
        // La ficha que cargó el profe a mano no tiene login: no puede ser la de un profe,
        // así que el filtro no la puede tocar.
        Activas(Ficha("Ana"));

        Assert.Single(await _service.PendientesAsync());
    }

    // ── Anotado a mano: el que ya viene y le pide otro día al profe en la cancha ──

    [Fact]
    public async Task Pendientes_AnotadoAMano_ConClase_Aparece()
    {
        // Es alumno (viene los martes) y le pidió los jueves hablando: sin esto no
        // había dónde registrarlo, porque a la espera solo se llegaba sin ninguna
        // clase o pidiendo cupo desde el portal.
        var ana = Anotada("Ana");
        Activas(ana);
        ConClase(ana);

        var item = Assert.Single(await _service.PendientesAsync());

        Assert.Equal("Ana", item.Nombre);
        Assert.Equal(nameof(MotivoEspera.LoAnotoElProfe), item.Motivo);
        Assert.Null(item.SolicitudId); // no hay pedido que rechazar
        Assert.Null(item.Clase);       // no dijo cuál: por eso lo anotó el profe
    }

    [Fact]
    public async Task Pendientes_AnotadoAMano_OrdenaPorCuandoLoAnotaste()
    {
        // La fila lleva la fecha de la MARCA, no la del alta de la ficha: el que hace
        // más tiempo que espera va primero, aunque su ficha sea la más nueva.
        var vieja = Anotada("Vieja");
        vieja.CreadoEl = DateTime.UtcNow.AddYears(-2);
        vieja.EnEsperaDesde = DateTime.UtcNow.AddMinutes(-1);
        var nueva = Anotada("Nueva");
        nueva.CreadoEl = DateTime.UtcNow;
        nueva.EnEsperaDesde = DateTime.UtcNow.AddDays(-30);
        Activas(vieja, nueva);
        ConClase(vieja, nueva);

        var filas = await _service.PendientesAsync();

        Assert.Equal("Nueva", filas[0].Nombre); // anotada hace 30 días
        Assert.Equal("Vieja", filas[1].Nombre);
    }

    [Fact]
    public async Task Pendientes_AnotadoAMano_SinClase_ApareceComoSinClase()
    {
        // Sin ninguna clase ya está esperando: la marca no agrega nada y gana el
        // motivo concreto, que es el que ofrece sacarlo de la academia.
        var ana = Anotada("Ana");
        Activas(ana);

        var item = Assert.Single(await _service.PendientesAsync());

        Assert.Equal(nameof(MotivoEspera.SinClase), item.Motivo);
    }

    [Fact]
    public async Task Pendientes_AnotadoAMano_YConPedidoDeCupo_ApareceUnaSolaVez()
    {
        // Lo anotaste vos Y además pidió cupo desde el portal: gana el pedido, que es
        // la fila con algo para resolver.
        var ana = Anotada("Ana");
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

        Assert.Equal(nameof(MotivoEspera.PidioCupo), item.Motivo);
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

    [Fact]
    public async Task Quitar_AnotadoAMano_ApagaLaMarcaYNoBorraLaFicha()
    {
        // Al que anotó el profe se lo saca de la espera sin tocarle nada más: es un
        // alumno igual que antes, solo que ya no está esperando otra clase.
        var ficha = Anotada("Ana");
        _alumnoRepo.Setup(a => a.ObtenerAsync(ficha.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);
        _alumnoRepo.Setup(a => a.FiltrarConClaseAsync(
                       It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync([ficha.Id]);

        await _service.QuitarDeEsperaAsync(ficha.Id);

        Assert.Null(ficha.EnEsperaDesde);
        _alumnoRepo.Verify(a => a.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
        _alumnoRepo.Verify(a => a.EliminarDefinitivoAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Marcar / desmarcar a mano ──

    [Fact]
    public async Task Marcar_AnotaConLaFechaDeHoy()
    {
        var ficha = Ficha("Ana");
        _alumnoRepo.Setup(a => a.ObtenerAsync(ficha.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);

        await _service.CambiarEsperaAsync(ficha.Id, true);

        Assert.NotNull(ficha.EnEsperaDesde);
        _alumnoRepo.Verify(a => a.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Marcar_DosVeces_NoPisaLaFechaOriginal()
    {
        // Si se pisara, volver a tocar el botón lo mandaría al final de la cola.
        var ficha = Anotada("Ana");
        var original = ficha.EnEsperaDesde;
        _alumnoRepo.Setup(a => a.ObtenerAsync(ficha.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);

        await _service.CambiarEsperaAsync(ficha.Id, true);

        Assert.Equal(original, ficha.EnEsperaDesde);
    }

    [Fact]
    public async Task Desmarcar_ApagaLaMarca()
    {
        var ficha = Anotada("Ana");
        _alumnoRepo.Setup(a => a.ObtenerAsync(ficha.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);

        await _service.CambiarEsperaAsync(ficha.Id, false);

        Assert.Null(ficha.EnEsperaDesde);
    }

    [Fact]
    public async Task Marcar_Staff_FichaDeOtroProfe_Lanza()
    {
        var yo = Guid.NewGuid();
        _usuario.Setup(u => u.EsStaff).Returns(true);
        _usuario.Setup(u => u.UserId).Returns(yo);
        var ajena = Ficha("Ajeno", Guid.NewGuid());
        _alumnoRepo.Setup(a => a.ObtenerAsync(ajena.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ajena);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CambiarEsperaAsync(ajena.Id, true));
        Assert.Null(ajena.EnEsperaDesde);
    }
}
