using Microsoft.EntityFrameworkCore;
using SwissBowling.Core;
using SwissBowling.Core.Import;
using SwissBowling.Server.Auth;
using SwissBowling.Server.Data;
using SwissBowling.Server.Infrastructure;

namespace SwissBowling.Server.Features.Import;

public static class ImportEndpoints
{
    public static void MapImport(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ImportRoutes.Runs).WithTags("Import").RequireAuthorization(Policies.Admin);

        group.MapGet("/", GetRuns);
        group.MapGet("/{id:guid}/entries", GetEntries);
        group.MapPost("/", StartRun).WithSummary("Import-/Aktualisierungslauf starten (Stammdaten, aktuelle Ranglisten, optional Archiv)");
    }

    /// <summary>Die letzten Läufe mit Summen – die Summen rechnet die Datenbank, Einträge werden nicht geladen.</summary>
    private static async Task<IResult> GetRuns(IDbContextFactory<AppDbContext> factory, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return TypedResults.Ok(await RunDtos(db.ImportRuns.OrderByDescending(r => r.StartedAt).Take(20)).ToListAsync(ct));
    }

    /// <summary>Protokolleinträge eines Laufs, neueste zuerst, seitenweise; optional nur Fehler.</summary>
    private static async Task<IResult> GetEntries(Guid id, IDbContextFactory<AppDbContext> factory, CancellationToken ct, bool errorsOnly = false, int page = 1, int pageSize = 50)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var query = db.ImportLogEntries.AsNoTracking().Where(e => e.RunId == id && (!errorsOnly || e.IsError));
        var size = Math.Clamp(pageSize, 1, 500);
        var total = await query.CountAsync(ct);
        var entries = await query.OrderByDescending(e => e.Time).Skip(Math.Max(0, page - 1) * size).Take(size).ToListAsync(ct);
        return TypedResults.Ok(new PagedResult<ImportLogEntryDto>(entries.Select(e => e.ToDto()).ToList(), total));
    }

    private static async Task<IResult> StartRun(StartImportRequest request, ImportRunner runner, IDbContextFactory<AppDbContext> factory, CancellationToken ct)
    {
        var id = await runner.StartAsync(request.IncludeArchive, ct);
        if (id is null)
        {
            return Problems.Conflict("Es läuft bereits ein Import.");
        }

        await using var db = await factory.CreateDbContextAsync(ct);
        return TypedResults.Ok(await RunDtos(db.ImportRuns.Where(r => r.Id == id)).FirstAsync(ct));
    }

    private static IQueryable<ImportRunDto> RunDtos(IQueryable<ImportRun> runs) =>
        runs.Select(r => new ImportRunDto(
            r.Id, r.StartedAt, r.FinishedAt, r.Status, r.IncludeArchive, r.CurrentStep,
            r.Entries.Sum(e => e.Clubs), r.Entries.Sum(e => e.Players), r.Entries.Sum(e => e.Results), r.Entries.Count(e => e.IsError)));
}
