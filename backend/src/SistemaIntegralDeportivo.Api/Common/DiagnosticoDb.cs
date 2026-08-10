namespace SistemaIntegralDeportivo.Api.Common;

/// <summary>
/// Cuánto trabajo de base hizo ESTE request: cuántos comandos SQL y cuántos ms
/// sumaron. Scoped, así que hay uno por request; lo llena
/// <see cref="ContadorDeQueriesInterceptor"/> y lo lee el middleware de tiempos.
///
/// Existe para poder distinguir "tarda porque hace 47 consultas" de "tarda porque
/// hace una pesada" de "tarda porque el JSON es enorme" — tres problemas con
/// arreglos distintos que desde afuera se ven igual.
/// </summary>
public class DiagnosticoDb
{
    public int Consultas { get; private set; }
    public double Milisegundos { get; private set; }

    public void Registrar(TimeSpan duracion)
    {
        Consultas++;
        Milisegundos += duracion.TotalMilliseconds;
    }
}
