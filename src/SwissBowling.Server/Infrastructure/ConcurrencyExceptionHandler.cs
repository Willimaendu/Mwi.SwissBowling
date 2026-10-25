using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SwissBowling.Server.Data;

namespace SwissBowling.Server.Infrastructure;

/// <summary>
/// Eine Zeile wurde zwischen Laden und Speichern von jemand anderem geändert oder gelöscht (z.B. durch einen laufenden
/// Importlauf). Statt eines 500ers bekommt der Client einen 409 mit verständlicher Meldung. Zur Diagnose wird geloggt,
/// welche Zeilen betroffen sind und ob sie in der Datenbank (noch) existieren.
/// </summary>
public sealed class ConcurrencyExceptionHandler(IDbContextFactory<AppDbContext> factory, ILogger<ConcurrencyExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is not DbUpdateConcurrencyException concurrency)
        {
            return false;
        }

        logger.LogWarning(exception, "Konflikt beim Speichern ({Path}): {Entries}", httpContext.Request.Path, await DescribeAsync(concurrency, ct));

        await Problems.Conflict("Die Daten wurden zwischenzeitlich geändert oder gelöscht – bitte die Seite neu laden und erneut speichern.")
            .ExecuteAsync(httpContext);
        return true;
    }

    private async Task<string> DescribeAsync(DbUpdateConcurrencyException exception, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var parts = new List<string>();
        foreach (var entry in exception.Entries.Take(10))
        {
            var key = entry.Metadata.FindPrimaryKey()?.Properties.Select(p => entry.Property(p.Name).CurrentValue).ToArray() ?? [];
            var exists = key.Length > 0 && await db.FindAsync(entry.Metadata.ClrType, key, ct) is not null;
            var modified = string.Join("/", entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name));
            parts.Add($"{entry.Metadata.ClrType.Name} {entry.State} Id={string.Join(",", key)} existiert={exists} geändert={modified}");
        }

        return string.Join("; ", parts);
    }
}
