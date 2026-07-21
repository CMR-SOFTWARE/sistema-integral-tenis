using Moq;
using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;
using SistemaIntegralDeportivo.Api.Services;

namespace SistemaIntegralDeportivo.Api.Tests.Services;

/// <summary>
/// Tests de la lógica de negocio del alta de alumno (TDD, ADR-0005).
/// Plan v2: el alta crea TAMBIÉN las credenciales (usuario + temporal) —
/// el registro es una sola vez. El repositorio e Identity están mockeados.
/// </summary>
public class AlumnoServiceTests
{
    private static readonly Guid UserIdNuevo = Guid.NewGuid();

    private readonly Mock<IAlumnoRepository> _repo;
    private readonly Mock<ICargoRepository> _cargos;
    private readonly Mock<ICredencialesService> _credenciales;
    private readonly Mock<ITurnoRepository> _turnos;
    private readonly Mock<IGrupoRepository> _grupos;
    private readonly Mock<IHorarioRepository> _horarios;
    private readonly Mock<IStaffService> _staff;
    private readonly AlumnoService _service;

    public AlumnoServiceTests()
    {
        _repo = new Mock<IAlumnoRepository>();
        _cargos = new Mock<ICargoRepository>();
        _credenciales = new Mock<ICredencialesService>();
        _turnos = new Mock<ITurnoRepository>();
        _grupos = new Mock<IGrupoRepository>();
        _horarios = new Mock<IHorarioRepository>();
        _staff = new Mock<IStaffService>();
        _staff.Setup(s => s.EsAsignableAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Por defecto: sin turnos futuros, sin grupos ni horarios individuales
        _turnos.Setup(t => t.ListarFuturosDeAlumnoAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        _turnos.Setup(t => t.ListarPorHorarioDesdeAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        _cargos.Setup(c => c.ListarPorTurnosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        _grupos.Setup(g => g.ListarMembresiasActivasDeAlumnoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        _horarios.Setup(h => h.ListarIndividualesDeAlumnoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);

        // Por defecto: el DNI no existe y AgregarAsync devuelve lo que recibe
        _repo.Setup(r => r.ExisteDniAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);
        _repo.Setup(r => r.ObtenerPorDniAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Alumno?)null);
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Alumno a, CancellationToken _) => a);

        // Por defecto: nadie debe nada y las credenciales salen bien
        _cargos.Setup(c => c.ListarImpagosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        _credenciales.Setup(c => c.CrearConTemporalAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new CredencialesCreadas(UserIdNuevo, "Temp1234AB"));

        _service = new AlumnoService(
            _repo.Object, _cargos.Object, _credenciales.Object,
            _turnos.Object, _grupos.Object, _horarios.Object, _staff.Object);
    }

    /// <summary>DTO válido de un alumno MAYOR de edad (base de los tests).</summary>
    private static CreateAlumnoDto AlumnoMayor() => new()
    {
        Nombre = "Juan",
        Apellido = "Pérez",
        Dni = "30111222",
        Telefono = "+5491155551234",
        Email = "juan@mail.com",
        FechaNacimiento = DateTime.UtcNow.AddYears(-30), // 30 años
        Categoria = CategoriaAlumno.Cuarta,
        ConsentimientoDatos = true,
    };

    /// <summary>DTO de un alumno MENOR (15 años), sin tutor por defecto.</summary>
    private static CreateAlumnoDto AlumnoMenor() => new()
    {
        Nombre = "Sofía",
        Apellido = "Gómez",
        Dni = "50333444",
        Telefono = "+5491155559876",
        Email = "sofia@mail.com",
        FechaNacimiento = DateTime.UtcNow.AddYears(-15), // 15 años
        Categoria = CategoriaAlumno.Septima,
    };

    private static TutorDto TutorValido() => new()
    {
        Nombre = "Marta",
        Apellido = "Gómez",
        Dni = "22555666",
        Telefono = "+5491144443333",
        Relacion = RelacionTutor.Madre,
    };

    // ─────────────────────────────────────────────
    // Regla del menor (modelo-alumnos.md §3.2, Ley 25.326)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CrearAsync_MenorSinTutor_LanzaReglaDeNegocio()
    {
        var dto = AlumnoMenor(); // sin tutor
        dto.ConsentimientoDatos = true;

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));

        // Y no tiene que haber intentado persistir NI crear usuario
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()), Times.Never);
        _credenciales.Verify(c => c.CrearConTemporalAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CrearAsync_MenorSinConsentimientoDeDatos_LanzaReglaDeNegocio()
    {
        var dto = AlumnoMenor();
        dto.Tutor = TutorValido();
        dto.ConsentimientoDatos = false; // el tutor NO consintió

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(dto));
    }

    [Fact]
    public async Task CrearAsync_MenorConTutorYConsentimiento_CreaYMarcaEsMenor()
    {
        var dto = AlumnoMenor();
        dto.Tutor = TutorValido();
        dto.ConsentimientoDatos = true;

        var result = await _service.CrearAsync(dto);

        Assert.True(result.Alumno.EsMenor);
        Assert.Equal("Sofía", result.Alumno.Nombre);
        _repo.Verify(r => r.AgregarAsync(
            It.Is<Alumno>(a => a.Tutor != null && a.Tutor.Dni == "22555666"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────
    // Credenciales: el alta crea el usuario con temporal (registro único)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CrearAsync_CreaLasCredenciales_YDevuelveLaTemporalUnaVez()
    {
        var result = await _service.CrearAsync(AlumnoMayor());

        Assert.Equal("Temp1234AB", result.PasswordTemporal);
        Assert.Equal("juan@mail.com", result.Email);
        // La ficha nace VINCULADA al usuario nuevo
        _repo.Verify(r => r.AgregarAsync(
            It.Is<Alumno>(a => a.UserId == UserIdNuevo),
            It.IsAny<CancellationToken>()), Times.Once);
        _credenciales.Verify(c => c.CrearConTemporalAsync(
            "juan@mail.com", "Juan", "Pérez", "30111222", "+5491155551234",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CrearAsync_EmailYaRegistrado_Lanza_YNoPersisteLaFicha()
    {
        _credenciales.Setup(c => c.CrearConTemporalAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new ReglaDeNegocioException("El email ya tiene una cuenta."));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(AlumnoMayor()));

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CrearAsync_DniDuplicado_NoLlegaACrearElUsuario()
    {
        // Orden anti-huérfanos: las reglas de la ficha se validan ANTES de Identity
        _repo.Setup(r => r.ExisteDniAsync("30111222", It.IsAny<CancellationToken>()))
             .ReturnsAsync(true);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(AlumnoMayor()));

        _credenciales.Verify(c => c.CrearConTemporalAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CrearAsync_SiFallaLaFicha_BorraElUsuarioCreado()
    {
        // Compensación: si la ficha explota (carrera del índice único), el
        // usuario recién creado no puede quedar huérfano
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new InvalidOperationException("índice único"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CrearAsync(AlumnoMayor()));

        _credenciales.Verify(c => c.EliminarAsync(UserIdNuevo, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────
    // Crear acceso (fichas viejas sin usuario)
    // ─────────────────────────────────────────────

    private Alumno FichaExistente(string? email = "juan@mail.com", Guid? userId = null)
    {
        var alumno = new Alumno
        {
            Nombre = "Juan", Apellido = "Pérez", Dni = "30111222",
            Telefono = "+5491155551234", Email = email,
            FechaNacimiento = DateTime.UtcNow.AddYears(-30),
            UserId = userId,
        };
        _repo.Setup(r => r.ObtenerAsync(alumno.Id, It.IsAny<CancellationToken>()))
             .ReturnsAsync(alumno);
        return alumno;
    }

    [Fact]
    public async Task CrearAcceso_FichaSinUsuario_VinculaYDevuelveLaTemporal()
    {
        var ficha = FichaExistente();

        var acceso = await _service.CrearAccesoAsync(ficha.Id, email: null);

        Assert.Equal("Temp1234AB", acceso.PasswordTemporal);
        Assert.Equal("juan@mail.com", acceso.Email); // usó el email de la ficha
        Assert.Equal(UserIdNuevo, ficha.UserId);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CrearAcceso_FichaSinEmail_ExigeElEmailDelBody_YLoGuarda()
    {
        var ficha = FichaExistente(email: null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAccesoAsync(ficha.Id, email: null));

        var acceso = await _service.CrearAccesoAsync(ficha.Id, email: "nuevo@mail.com");
        Assert.Equal("nuevo@mail.com", acceso.Email);
        Assert.Equal("nuevo@mail.com", ficha.Email); // quedó en la ficha
    }

    [Fact]
    public async Task CrearAcceso_FichaYaConUsuario_Lanza()
    {
        var ficha = FichaExistente(userId: Guid.NewGuid());

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearAccesoAsync(ficha.Id, email: null));
    }

    // ─────────────────────────────────────────────
    // Crear VINCULADO (aprobación de solicitudes): sin credenciales nuevas
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CrearVinculado_CreaLaFichaConElUserId_SinTocarIdentity()
    {
        var userId = Guid.NewGuid();

        var creado = await _service.CrearVinculadoAsync(AlumnoMayor(), userId);

        Assert.Equal("Juan", creado.Nombre);
        _repo.Verify(r => r.AgregarAsync(
            It.Is<Alumno>(a => a.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);
        _credenciales.Verify(c => c.CrearConTemporalAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CrearVinculado_DniExistenteSinUsuario_VinculaEsaFicha_SinDuplicar()
    {
        // El profe ya lo tenía cargado: el reemplazo elegante del reclamo
        var userId = Guid.NewGuid();
        var existente = FichaExistente();
        _repo.Setup(r => r.ObtenerPorDniAsync("30111222", It.IsAny<CancellationToken>()))
             .ReturnsAsync(existente);

        var creado = await _service.CrearVinculadoAsync(AlumnoMayor(), userId);

        Assert.Equal(existente.Id, creado.Id);
        Assert.Equal(userId, existente.UserId);
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CrearVinculado_DniDeOtraCuenta_Lanza()
    {
        var existente = FichaExistente(userId: Guid.NewGuid()); // ya es de otro
        _repo.Setup(r => r.ObtenerPorDniAsync("30111222", It.IsAny<CancellationToken>()))
             .ReturnsAsync(existente);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.CrearVinculadoAsync(AlumnoMayor(), Guid.NewGuid()));
    }

    // ─────────────────────────────────────────────
    // Editar la ficha (el profe corrige datos del alumno)
    // ─────────────────────────────────────────────

    private static UpdateAlumnoDto Edicion() => new()
    {
        Nombre = "Juan Carlos",
        Apellido = "Pérez",
        Dni = "30111222",
        Telefono = "+5491199998888",
        Email = "nuevo@mail.com",
        FechaNacimiento = DateTime.UtcNow.AddYears(-30),
        Categoria = CategoriaAlumno.Tercera,
        Modalidad = ModalidadPago.PorClase,
        Notas = "Mejoró el revés",
    };

    [Fact]
    public async Task EditarAsync_ActualizaLosDatos_YDevuelveLaFicha()
    {
        var ficha = FichaExistente();

        var result = await _service.EditarAsync(ficha.Id, Edicion());

        Assert.Equal("Juan Carlos", result.Nombre);
        Assert.Equal("Tercera", result.Categoria);
        Assert.Equal("+5491199998888", ficha.Telefono);
        Assert.Equal(ModalidadPago.PorClase, ficha.Modalidad);
        Assert.Equal("Mejoró el revés", ficha.Notas);
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EditarAsync_MismoDni_NoSeQuejaDeDuplicado()
    {
        // Guardar sin cambiar el DNI no puede chocar contra uno mismo
        var ficha = FichaExistente();
        _repo.Setup(r => r.ObtenerPorDniAsync("30111222", It.IsAny<CancellationToken>()))
             .ReturnsAsync(ficha); // el dueño del DNI es él mismo

        var result = await _service.EditarAsync(ficha.Id, Edicion());

        Assert.Equal("30111222", result.Dni);
    }

    [Fact]
    public async Task EditarAsync_DniDeOtroAlumno_Lanza()
    {
        var ficha = FichaExistente();
        var otro = new Alumno
        {
            Nombre = "Otro", Apellido = "Alumno", Dni = "99999999",
            Telefono = "+549110000", FechaNacimiento = DateTime.UtcNow.AddYears(-20),
        };
        _repo.Setup(r => r.ObtenerPorDniAsync("99999999", It.IsAny<CancellationToken>()))
             .ReturnsAsync(otro);
        var dto = Edicion();
        dto.Dni = "99999999";

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.EditarAsync(ficha.Id, dto));

        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EditarAsync_FechaQueLoVuelveMenorSinTutor_Lanza()
    {
        // Corregir mal la fecha no puede dejar un menor sin tutor (Ley 25.326)
        var ficha = FichaExistente(); // sin tutor
        var dto = Edicion();
        dto.FechaNacimiento = DateTime.UtcNow.AddYears(-15);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.EditarAsync(ficha.Id, dto));
    }

    [Fact]
    public async Task EditarAsync_Inexistente_Lanza()
    {
        _repo.Setup(r => r.ObtenerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Alumno?)null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _service.EditarAsync(Guid.NewGuid(), Edicion()));
    }

    // ─────────────────────────────────────────────
    // Estado ↔ calendario: pausar/dar de baja saca del calendario
    // (pausa GUARDA el lugar; baja lo LIBERA)
    // ─────────────────────────────────────────────

    private static readonly Guid OtroAlumno = Guid.NewGuid();

    /// <summary>Turno grupal futuro con el alumno + otro compañero.</summary>
    private Turno TurnoGrupalFuturo(Alumno mio, int enDias = 3)
    {
        var turno = new Turno
        {
            HorarioId = Guid.NewGuid(),
            Fecha = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(enDias),
            HoraInicio = new TimeOnly(18, 0),
            DuracionMinutos = 60,
        };
        turno.Participantes.Add(new TurnoParticipante { Turno = turno, AlumnoId = mio.Id });
        turno.Participantes.Add(new TurnoParticipante { Turno = turno, AlumnoId = OtroAlumno });
        return turno;
    }

    [Fact]
    public async Task Pausar_LoSaca_YRecalculaLaCuotaDelResto()
    {
        // El divisor baja (÷3 en vez de ÷4): los cargos impagos del turno se
        // invalidan (mío Y del compañero) para que la liquidación los
        // regenere con el nuevo divisor. Sin esto, el compañero quedaba con
        // la cuota vieja más barata (era el bug reportado, en su forma inversa).
        var ficha = FichaExistente();
        var turno = TurnoGrupalFuturo(ficha);
        var miCargoImpago = new Cargo
        {
            AlumnoId = ficha.Id, TurnoId = turno.Id, Tipo = TipoCargo.Clase,
            Concepto = "Clase grupal", Monto = 4_000m, Fecha = turno.Fecha,
        };
        var cargoDelOtro = new Cargo
        {
            AlumnoId = OtroAlumno, TurnoId = turno.Id, Tipo = TipoCargo.Clase,
            Concepto = "Clase grupal", Monto = 4_000m, Fecha = turno.Fecha,
        };
        _turnos.Setup(t => t.ListarFuturosDeAlumnoAsync(ficha.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([turno]);
        _cargos.Setup(c => c.ListarPorTurnosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([miCargoImpago, cargoDelOtro]);

        await _service.CambiarEstadoAsync(ficha.Id, EstadoAlumno.Suspendido);

        // Sale del roster (el turno SIGUE para el compañero)
        _turnos.Verify(t => t.QuitarParticipante(
            It.Is<TurnoParticipante>(p => p.AlumnoId == ficha.Id)), Times.Once);
        _turnos.Verify(t => t.Eliminar(It.IsAny<Turno>()), Times.Never);
        // Ambos cargos impagos se invalidan para recalcularse con ÷3
        _cargos.Verify(c => c.Eliminar(miCargoImpago), Times.Once);
        _cargos.Verify(c => c.Eliminar(cargoDelOtro), Times.Once);
    }

    [Fact]
    public async Task Pausar_TurnoConSuCargoYaPAGADO_NoSeToca()
    {
        // Plata cobrada = intocable (regla de la casa)
        var ficha = FichaExistente();
        var turno = TurnoGrupalFuturo(ficha);
        var miCargoPagado = new Cargo
        {
            AlumnoId = ficha.Id, TurnoId = turno.Id, Tipo = TipoCargo.Clase,
            Concepto = "Clase grupal", Monto = 4_000m, Fecha = turno.Fecha,
            PagadoEl = DateTime.UtcNow, MedioPago = MedioPago.Efectivo,
        };
        _turnos.Setup(t => t.ListarFuturosDeAlumnoAsync(ficha.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([turno]);
        _cargos.Setup(c => c.ListarPorTurnosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([miCargoPagado]);

        await _service.CambiarEstadoAsync(ficha.Id, EstadoAlumno.Suspendido);

        _turnos.Verify(t => t.QuitarParticipante(It.IsAny<TurnoParticipante>()), Times.Never);
        _cargos.Verify(c => c.Eliminar(It.IsAny<Cargo>()), Times.Never);
    }

    [Fact]
    public async Task Pausar_TurnoIndividual_SeElimina_EnVezDeQuedarVacio()
    {
        var ficha = FichaExistente();
        var individual = new Turno
        {
            HorarioId = Guid.NewGuid(),
            Fecha = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2),
            HoraInicio = new TimeOnly(9, 0),
            DuracionMinutos = 60,
        };
        individual.Participantes.Add(new TurnoParticipante { Turno = individual, AlumnoId = ficha.Id });
        _turnos.Setup(t => t.ListarFuturosDeAlumnoAsync(ficha.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([individual]);

        await _service.CambiarEstadoAsync(ficha.Id, EstadoAlumno.Suspendido);

        // Era el único: el turno entero se va (libera el slot en el calendario)
        _turnos.Verify(t => t.Eliminar(individual), Times.Once);
    }

    [Fact]
    public async Task Pausar_NO_TocaSusGruposNiSuHorarioIndividual()
    {
        // La PAUSA le guarda el lugar (lesión/viaje): vuelve solo al reactivarlo
        var ficha = FichaExistente();
        var membresia = new AlumnoGrupo { AlumnoId = ficha.Id, GrupoId = Guid.NewGuid() };
        var suHorario = new Horario
        {
            CanchaId = Guid.NewGuid(), AlumnoId = ficha.Id, Dia = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(9, 0), DuracionMinutos = 60,
        };
        _grupos.Setup(g => g.ListarMembresiasActivasDeAlumnoAsync(ficha.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync([membresia]);
        _horarios.Setup(h => h.ListarIndividualesDeAlumnoAsync(ficha.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync([suHorario]);

        await _service.CambiarEstadoAsync(ficha.Id, EstadoAlumno.Suspendido);

        Assert.Null(membresia.FechaBaja); // sigue en el grupo: nadie le toma el lugar
        Assert.True(suHorario.Activo);    // su horario sigue reservado
    }

    [Fact]
    public async Task Baja_LiberaElLugar_SacaDeGruposYDesactivaSuHorario()
    {
        var ficha = FichaExistente();
        var membresia = new AlumnoGrupo { AlumnoId = ficha.Id, GrupoId = Guid.NewGuid() };
        var suHorario = new Horario
        {
            CanchaId = Guid.NewGuid(), AlumnoId = ficha.Id, Dia = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(9, 0), DuracionMinutos = 60,
        };
        _grupos.Setup(g => g.ListarMembresiasActivasDeAlumnoAsync(ficha.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync([membresia]);
        _horarios.Setup(h => h.ListarIndividualesDeAlumnoAsync(ficha.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync([suHorario]);

        await _service.DarDeBajaAsync(ficha.Id);

        Assert.Equal(EstadoAlumno.Inactivo, ficha.Estado);
        Assert.NotNull(membresia.FechaBaja); // libera el cupo del grupo
        Assert.False(suHorario.Activo);      // libera el slot de la cancha
    }

    [Fact]
    public async Task Reactivar_LoRepone_EnLosTurnosFuturosDeSusGrupos()
    {
        var ficha = FichaExistente(userId: null);
        ficha.Estado = EstadoAlumno.Suspendido;
        var grupoId = Guid.NewGuid();
        var horarioGrupal = new Horario
        {
            CanchaId = Guid.NewGuid(), GrupoId = grupoId, Dia = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60,
        };
        // Turno futuro del grupo SIN él (lo sacamos al pausarlo)
        var turnoSinMi = new Turno
        {
            HorarioId = horarioGrupal.Id,
            Fecha = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3),
            HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60,
        };
        turnoSinMi.Participantes.Add(new TurnoParticipante { Turno = turnoSinMi, AlumnoId = OtroAlumno });

        _grupos.Setup(g => g.ListarMembresiasActivasDeAlumnoAsync(ficha.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync([new AlumnoGrupo { AlumnoId = ficha.Id, GrupoId = grupoId }]);
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([horarioGrupal]);
        _turnos.Setup(t => t.ListarPorHorarioDesdeAsync(horarioGrupal.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([turnoSinMi]);

        // El turno tenía un cargo impago del compañero calculado con divisor
        // viejo (÷1, sin el que vuelve): al reponerlo debe invalidarse para
        // recalcularse ÷2 — ESTE es el bug que reportó Lucas.
        var cargoViejoDelOtro = new Cargo
        {
            AlumnoId = OtroAlumno, TurnoId = turnoSinMi.Id, Tipo = TipoCargo.Clase,
            Concepto = "Clase grupal", Monto = 8_000m, Fecha = turnoSinMi.Fecha,
        };
        _cargos.Setup(c => c.ListarPorTurnosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([cargoViejoDelOtro]);

        await _service.CambiarEstadoAsync(ficha.Id, EstadoAlumno.Activo);

        Assert.Contains(turnoSinMi.Participantes, p => p.AlumnoId == ficha.Id); // volvió al roster
        _cargos.Verify(c => c.Eliminar(cargoViejoDelOtro), Times.Once);         // se recalcula
        // La reconciliación no persiste: el cambio de estado hace un único
        // GuardarCambios (mismo DbContext) al final
        _repo.Verify(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reactivar_NoDuplicaSiYaEstabaEnElTurno()
    {
        var ficha = FichaExistente();
        ficha.Estado = EstadoAlumno.Suspendido;
        var grupoId = Guid.NewGuid();
        var horarioGrupal = new Horario
        {
            CanchaId = Guid.NewGuid(), GrupoId = grupoId, Dia = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(18, 0), DuracionMinutos = 60,
        };
        var turnoConMi = TurnoGrupalFuturo(ficha);
        _grupos.Setup(g => g.ListarMembresiasActivasDeAlumnoAsync(ficha.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync([new AlumnoGrupo { AlumnoId = ficha.Id, GrupoId = grupoId }]);
        _horarios.Setup(h => h.ListarActivosAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([horarioGrupal]);
        _turnos.Setup(t => t.ListarPorHorarioDesdeAsync(horarioGrupal.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([turnoConMi]);

        await _service.CambiarEstadoAsync(ficha.Id, EstadoAlumno.Activo);

        Assert.Single(turnoConMi.Participantes, p => p.AlumnoId == ficha.Id);
    }

    // ─────────────────────────────────────────────
    // Consentimiento con timestamp + unicidad de DNI (reglas previas)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CrearAsync_ConConsentimientos_GuardaCuandoSeOtorgaron()
    {
        var dto = AlumnoMayor();
        dto.ConsentimientoWhatsapp = true;

        Alumno? persistido = null;
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()))
             .Callback((Alumno a, CancellationToken _) => persistido = a)
             .ReturnsAsync((Alumno a, CancellationToken _) => a);

        await _service.CrearAsync(dto);

        Assert.NotNull(persistido);
        Assert.NotNull(persistido!.ConsentimientoDatosEl);    // quedó el timestamp
        Assert.NotNull(persistido.ConsentimientoWhatsappEl);
    }

    [Fact]
    public async Task CrearAsync_DniYaExistenteEnElTenant_LanzaReglaDeNegocio()
    {
        _repo.Setup(r => r.ExisteDniAsync("30111222", It.IsAny<CancellationToken>()))
             .ReturnsAsync(true); // ya hay un alumno con ese DNI

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _service.CrearAsync(AlumnoMayor()));

        _repo.Verify(r => r.AgregarAsync(It.IsAny<Alumno>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
