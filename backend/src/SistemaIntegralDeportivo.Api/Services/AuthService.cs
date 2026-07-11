using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Dtos;
using SistemaIntegralDeportivo.Api.Models;
using SistemaIntegralDeportivo.Api.Repositories;

namespace SistemaIntegralDeportivo.Api.Services;

public class AuthService : IAuthService
{
    private readonly IAlumnoRepository _alumnos;
    private readonly ITenantRepository _tenants;
    private readonly ITokenService _tokens;

    public AuthService(IAlumnoRepository alumnos, ITenantRepository tenants, ITokenService tokens)
    {
        _alumnos = alumnos;
        _tenants = tenants;
        _tokens = tokens;
    }

    public async Task<SesionDto> ArmarSesionAsync(
        Usuario usuario, bool incluirToken, CancellationToken ct = default)
    {
        var esProfesor = await _tenants.EsDuenioAsync(usuario.Id, ct);

        // Una ficha por usuario en el prototipo (multi-membresía: fase futura).
        // Si ya reclamó una, no se ofrecen más.
        var vinculada = await _alumnos.ObtenerPorUserIdAsync(usuario.Id, ct);
        var porReclamar = vinculada is null
            ? await CandidatasAsync(usuario, ct)
            : [];

        return new SesionDto
        {
            Token = incluirToken ? _tokens.Generar(usuario, esProfesor) : null,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email ?? string.Empty,
            EsProfesor = esProfesor,
            Alumno = vinculada is null ? null : Mapear(vinculada),
            FichasPorReclamar = porReclamar.Select(Mapear).ToList(),
        };
    }

    public async Task ReclamarFichaAsync(
        Usuario usuario, Guid alumnoId, CancellationToken ct = default)
    {
        // La ficha tiene que estar entre MIS candidatas: libre (sin UserId)
        // y coincidente por DNI/teléfono. Todo lo demás es un reclamo inválido.
        var candidatas = await CandidatasAsync(usuario, ct);
        var ficha = candidatas.FirstOrDefault(a => a.Id == alumnoId)
            ?? throw new ReglaDeNegocioException(
                "La ficha no existe, ya fue reclamada o no coincide con tus datos (DNI/teléfono).");

        ficha.UserId = usuario.Id;
        await _alumnos.GuardarCambiosAsync(ct);
    }

    private async Task<IReadOnlyList<Alumno>> CandidatasAsync(Usuario usuario, CancellationToken ct)
    {
        // Sin DNI ni teléfono no hay contra qué matchear
        if (string.IsNullOrWhiteSpace(usuario.Dni) && string.IsNullOrWhiteSpace(usuario.PhoneNumber))
            return [];

        return await _alumnos.BuscarReclamablesAsync(usuario.Dni, usuario.PhoneNumber, ct);
    }

    private static FichaDto Mapear(Alumno a) => new()
    {
        AlumnoId = a.Id,
        Nombre = a.Nombre,
        Apellido = a.Apellido,
        Club = a.Tenant?.Nombre ?? string.Empty,
    };
}
