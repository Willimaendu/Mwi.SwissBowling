using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mwi.SwissBowling.Core.Http;

namespace Mwi.SwissBowling.Core.Hosting;

public sealed class SafeExceptionHandler(ILogger<SafeExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is RequestRejectedException rejected)
        {
            await Results.Problem(statusCode: rejected.StatusCode, title: rejected.Message).ExecuteAsync(httpContext);
            return true;
        }
        var conflict = exception is DbUpdateConcurrencyException || exception is DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: "23505" or "23503" or "23514" } };
        logger.LogError("Request failed with {ErrorType}; trace {TraceId}", exception.GetType().Name, httpContext.TraceIdentifier);
        await Results.Problem(statusCode: conflict ? 409 : 500,
            title: conflict ? "Die Daten wurden verändert oder widersprechen einer bestehenden Zuordnung." : "Die Anfrage konnte nicht verarbeitet werden.",
            extensions: new Dictionary<string, object?> { ["traceId"] = httpContext.TraceIdentifier }).ExecuteAsync(httpContext);
        return true;
    }
}
