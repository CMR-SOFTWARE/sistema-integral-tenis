using SistemaIntegralDeportivo.Api.Models;

namespace SistemaIntegralDeportivo.Api.Common;

/// <summary>Escala de la categoría (el género queda implícito en el valor).</summary>
public enum EscalaCategoria
{
    Varones,
    Damas,
    Sin, // SinCategoria: no pertenece a ninguna escala
}

/// <summary>
/// Reglas de las categorías deportivas: en qué escala (varones/damas) está cada
/// una, su posición dentro de la escala, y la compatibilidad para armar grupos.
/// El género es IMPLÍCITO en el valor (una 4ta es de varones; una B1, de damas).
/// </summary>
public static class Categorias
{
    public static EscalaCategoria Escala(CategoriaAlumno cat) => cat switch
    {
        CategoriaAlumno.Primera or CategoriaAlumno.Segunda or CategoriaAlumno.Tercera
            or CategoriaAlumno.Cuarta or CategoriaAlumno.Quinta or CategoriaAlumno.Sexta
            => EscalaCategoria.Varones,
        CategoriaAlumno.A or CategoriaAlumno.B1 or CategoriaAlumno.B2
            or CategoriaAlumno.C1 or CategoriaAlumno.C2 or CategoriaAlumno.D
            => EscalaCategoria.Damas,
        _ => EscalaCategoria.Sin,
    };

    /// <summary>Posición DENTRO de la escala (0 = la mejor … 5), o -1 si no tiene escala.</summary>
    public static int Rank(CategoriaAlumno cat) => cat switch
    {
        CategoriaAlumno.Primera => 0, CategoriaAlumno.Segunda => 1, CategoriaAlumno.Tercera => 2,
        CategoriaAlumno.Cuarta => 3, CategoriaAlumno.Quinta => 4, CategoriaAlumno.Sexta => 5,
        CategoriaAlumno.A => 0, CategoriaAlumno.B1 => 1, CategoriaAlumno.B2 => 2,
        CategoriaAlumno.C1 => 3, CategoriaAlumno.C2 => 4, CategoriaAlumno.D => 5,
        _ => -1,
    };

    /// <summary>
    /// ¿El alumno puede sumarse a un grupo de esa categoría? El grupo sin categoría
    /// (null o SinCategoria) es ABIERTO a todos; el alumno todavía sin evaluar
    /// (SinCategoria) solo entra a los abiertos. Si no, tienen que ser de la MISMA
    /// escala (varones/damas) y a lo sumo una categoría de diferencia (±1): una 4ta
    /// llega a 3ra/4ta/5ta; una B1 a A/B1/B2; una 4ta NO matchea con una C1.
    /// </summary>
    public static bool EsCompatible(CategoriaAlumno? grupo, CategoriaAlumno alumno)
    {
        if (grupo is null || grupo == CategoriaAlumno.SinCategoria) return true;
        if (alumno == CategoriaAlumno.SinCategoria) return false;
        return Escala(grupo.Value) == Escala(alumno)
            && Math.Abs(Rank(grupo.Value) - Rank(alumno)) <= 1;
    }
}
