using SistemaIntegralDeportivo.Api.Common;
using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Tests.Common;

/// <summary>
/// Compatibilidad categoría↔grupo (Tema C): ±1 DENTRO de cada escala (varones
/// 1ra-6ta, damas A-D); cross-escala incompatible; sin categoría = abierto.
/// </summary>
public class CategoriasTests
{
    [Theory]
    // Varones: misma o adyacente OK; a 2 de distancia, no
    [InlineData(CategoriaAlumno.Cuarta, CategoriaAlumno.Cuarta, true)]
    [InlineData(CategoriaAlumno.Cuarta, CategoriaAlumno.Tercera, true)]
    [InlineData(CategoriaAlumno.Cuarta, CategoriaAlumno.Quinta, true)]
    [InlineData(CategoriaAlumno.Cuarta, CategoriaAlumno.Segunda, false)]
    [InlineData(CategoriaAlumno.Primera, CategoriaAlumno.Cuarta, false)]
    // Damas: misma o adyacente OK
    [InlineData(CategoriaAlumno.B1, CategoriaAlumno.A, true)]
    [InlineData(CategoriaAlumno.B1, CategoriaAlumno.B2, true)]
    [InlineData(CategoriaAlumno.A, CategoriaAlumno.C1, false)]
    // Cross-escala (varones ↔ damas): nunca compatible, aunque el rank coincida
    [InlineData(CategoriaAlumno.Cuarta, CategoriaAlumno.C1, false)]
    [InlineData(CategoriaAlumno.B1, CategoriaAlumno.Segunda, false)]
    public void EsCompatible_RespetaEscalaYAdyacencia(
        CategoriaAlumno grupo, CategoriaAlumno alumno, bool esperado)
    {
        Assert.Equal(esperado, Categorias.EsCompatible(grupo, alumno));
    }

    [Fact]
    public void EsCompatible_GrupoSinCategoria_AbiertoATodos()
    {
        Assert.True(Categorias.EsCompatible(null, CategoriaAlumno.Cuarta));
        Assert.True(Categorias.EsCompatible(CategoriaAlumno.SinCategoria, CategoriaAlumno.C1));
        Assert.True(Categorias.EsCompatible(null, CategoriaAlumno.SinCategoria));
    }

    [Fact]
    public void EsCompatible_AlumnoSinCategoria_SoloGruposAbiertos()
    {
        Assert.False(Categorias.EsCompatible(CategoriaAlumno.Cuarta, CategoriaAlumno.SinCategoria));
        Assert.False(Categorias.EsCompatible(CategoriaAlumno.B1, CategoriaAlumno.SinCategoria));
        Assert.True(Categorias.EsCompatible(null, CategoriaAlumno.SinCategoria));
    }
}
