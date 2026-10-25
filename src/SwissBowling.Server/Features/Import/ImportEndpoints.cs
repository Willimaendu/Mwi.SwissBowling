using Microsoft.EntityFrameworkCore;
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
        group.MapPost("/", StartRun).WithSummary("Import-/Aktualisierungslauf starten (Stammdaten, aktuelle Ranglisten, optional Archiv)");
    }

    private static async Task<IResult> GetRuns(IDbContextFactory<AppDbContext> factory, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var runs = await db.ImportRuns.AsNoTracking().Include(r => r.Entries).OrderByDescending(r => r.StartedAt).Take(20).ToListAsync(ct);
        return TypedResults.Ok(runs.Select(r => r.ToDto()));
    }

    private static async Task<IResult> StartRun(StartImportRequest request, ImportRunner runner, IDbContextFactory<AppDbContext> factory, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var id = await runner.StartAsync(request.IncludeArchive, ct);
        if (id is null)
        {
            return Problems.Conflict("Es läuft bereits ein Import.");
        }

        var run = await db.ImportRuns.AsNoTracking().Include(r => r.Entries).FirstAsync(r => r.Id == id, ct);
        return TypedResults.Ok(run.ToDto());
    }
}
