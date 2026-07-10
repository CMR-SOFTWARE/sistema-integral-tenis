using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class HorarioService : IHorarioService
{
    private readonly IHorarioRepository _horarios;
    private readonly ITurnoRepository _turnos;
    private readonly ICargoRepository _cargos;

    public HorarioService(IHorarioRepository horarios, ITurnoRepository turnos, ICargoRepository cargos)
    {
        _horarios = horarios;
        _turnos = turnos;
        _cargos = cargos;
    }

    public async Task<HorarioResponseDto> CrearAsync(CreateHorarioDto dto, CancellationToken ct = default)
    {
        // Regla: grupal XOR individual — exactamente uno de los dos
        var tieneGrupo = dto.GrupoId is not null;
        var tieneAlumno = dto.AlumnoId is not null;
        if (tieneGrupo == tieneAlumno) // ambos o ninguno
            throw new ReglaDeNegocioException(
                "El horario debe apuntar a un grupo O a un alumno individual (exactamente uno).");

        // Regla: nadie toma clases NUEVAS con la cuota vencida (las ya asignadas siguen)
        if (dto.AlumnoId is not null)
        {
            var impagos = await _cargos.ListarImpagosAsync([dto.AlumnoId.Value], ct);
            if (CuotaService.TieneDeudaVencida(impagos, DateOnly.FromDateTime(DateTime.UtcNow)))
                throw new ReglaDeNegocioException(
                    "El alumno tiene la cuota vencida: registrá el pago en Cuotas antes de asignarle clases nuevas.");
        }

        // Regla: sin solapamiento en la MISMA cancha (mismo día, rangos que se pisan)
        var fin = dto.HoraInicio.AddMinutes(dto.DuracionMinutos);
        var delDia = await _horarios.ListarPorCanchaYDiaAsync(dto.CanchaId, dto.Dia, ct);
        var pisado = delDia.FirstOrDefault(h =>
            dto.HoraInicio < h.HoraInicio.AddMinutes(h.DuracionMinutos) && h.HoraInicio < fin);
        if (pisado is not null)
            throw new ReglaDeNegocioException(
                $"Se superpone con otro horario de esa cancha ({pisado.HoraInicio:HH\\:mm}, {pisado.DuracionMinutos}').");

        var horario = new Horario
        {
            CanchaId = dto.CanchaId,
            GrupoId = dto.GrupoId,
            AlumnoId = dto.AlumnoId,
            Dia = dto.Dia,
            HoraInicio = dto.HoraInicio,
            DuracionMinutos = dto.DuracionMinutos,
            // TenantId lo asigna el repositorio
        };

        var creado = await _horarios.AgregarAsync(horario, ct);
        return Mapear(creado);
    }

    public async Task<IReadOnlyList<HorarioResponseDto>> ListarAsync(CancellationToken ct = default)
    {
        var horarios = await _horarios.ListarActivosAsync(ct);
        return horarios.Select(Mapear).ToList();
    }

    public async Task DesactivarAsync(Guid id, CancellationToken ct = default)
    {
        var horario = await _horarios.ObtenerAsync(id, ct)
            ?? throw new ReglaDeNegocioException("El horario no existe.");

        horario.Activo = false;

        // Lo pasado es historia y no se toca. Lo futuro (≥ hoy) se limpia para
        // no dejar facturado algo que ya no va a ocurrir — salvo turnos con
        // algún cargo PAGADO: la plata cobrada no se rompe.
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var futuros = await _turnos.ListarPorHorarioDesdeAsync(id, hoy, ct);
        if (futuros.Count > 0)
        {
            var cargos = await _cargos.ListarPorTurnosAsync(futuros.Select(t => t.Id).ToList(), ct);
            var porTurno = cargos.ToLookup(c => c.TurnoId!.Value);
            foreach (var turno in futuros)
            {
                if (porTurno[turno.Id].Any(c => c.PagadoEl is not null)) continue;

                foreach (var cargo in porTurno[turno.Id])
                    _cargos.Eliminar(cargo);
                _turnos.Eliminar(turno);
            }
        }

        await _horarios.GuardarCambiosAsync(ct); // mismo DbContext: persiste todo junto
    }

    private static HorarioResponseDto Mapear(Horario h) => new()
    {
        Id = h.Id,
        Titulo = h.Grupo?.Nombre
            ?? (h.Alumno is not null ? $"{h.Alumno.Nombre} {h.Alumno.Apellido} (individual)" : string.Empty),
        Categoria = h.Grupo?.Categoria?.ToString() ?? h.Alumno?.Categoria.ToString(),
        EsIndividual = h.AlumnoId is not null,
        CanchaId = h.CanchaId,
        Cancha = h.Cancha?.Nombre ?? string.Empty,
        Sede = h.Cancha?.Sede?.Nombre ?? string.Empty,
        Dia = h.Dia,
        HoraInicio = h.HoraInicio,
        DuracionMinutos = h.DuracionMinutos,
        Activo = h.Activo,
    };
}
