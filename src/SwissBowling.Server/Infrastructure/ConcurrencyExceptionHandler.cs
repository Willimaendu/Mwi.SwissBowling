using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace SwissBowling.Server.Infrastructure;

/// <summary>
/// Eine Zeile wurde zwischen Laden und Speichern von jemand anderem geändert oder gelöscht (z.B. durch einen laufenden
/// Importlauf). Statt eines 500ers bekommt der Client einen 409 mit verständlicher Meldung.
/// </summary>
public sealed class ConcurrencyExceptionHandler(ILogger<ConcurrencyExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is not DbUpdateConcurrencyException concurrency)
        {
            return false;
        }

        var entries = string.Join(", ", concurrency.Entries.Take(10).Select(e => $"{e.Metadata.ClrType.Name} ({e.State})"));
        logger.LogWarning(exception, "Konflikt beim Speichern ({Path}): {Entries}", httpContext.Request.Path, entries);

        await Problems.Conflict("Die Daten wurden zwischenzeitlich geändert oder gelöscht – bitte die Seite neu laden und erneut speichern.")
            .ExecuteAsync(httpContext);
        return true;
    }
}
