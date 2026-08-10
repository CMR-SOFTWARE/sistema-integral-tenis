using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SistemaIntegralDeportivo.Api.Common;

/// <summary>
/// Anota en el <see cref="DiagnosticoDb"/> del request cada comando SQL que EF
/// ejecuta y cuánto tardó. EF ya trae la duración medida en <c>eventData.Duration</c>,
/// así que acá no se cronometra nada: el costo es sumar dos números.
///
/// Se engancha en el DbContext desde Program.cs. Como el interceptor es scoped
/// —igual que el DbContext y que el contador—, cada request suma en su propio
/// contador sin pisar a los demás.
/// </summary>
public class ContadorDeQueriesInterceptor : DbCommandInterceptor
{
    private readonly DiagnosticoDb _diagnostico;

    public ContadorDeQueriesInterceptor(DiagnosticoDb diagnostico)
    {
        _diagnostico = diagnostico;
    }

    // Los tres tipos de comando: leer (SELECT), escribir (INSERT/UPDATE/DELETE) y
    // escalar. Hay que cubrir los tres o el conteo miente en las pantallas que
    // además escriben (la agenda genera turnos al leerla).

    public override DbDataReader ReaderExecuted(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        _diagnostico.Registrar(eventData.Duration);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        _diagnostico.Registrar(eventData.Duration);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(
        DbCommand command, CommandExecutedEventData eventData, int result)
    {
        _diagnostico.Registrar(eventData.Duration);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        _diagnostico.Registrar(eventData.Duration);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(
        DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        _diagnostico.Registrar(eventData.Duration);
        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result,
        CancellationToken cancellationToken = default)
    {
        _diagnostico.Registrar(eventData.Duration);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }
}
