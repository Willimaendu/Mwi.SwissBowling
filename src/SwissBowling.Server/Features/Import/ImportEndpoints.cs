using Microsoft.EntityFrameworkCore;
using SwissBowling.Core.Import;
using SwissBowling.Server.Auth;
using SwissBowling.Server.Data;

namespace SwissBowling.Server.Features.Import;

public static class ImportEndpoints
{
    public static void MapImport(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ImportRoutes.Available).WithTags("Import UVweb").RequireAuthorization(Policies.Admin);

        group.MapGet("/", GetCurrent);
        group.MapPost("/{msId:int}", ImportCurrent);
        group.MapGet("/associations", GetAssociations);
        group.MapGet("/archive", GetArchive);
        group.MapPost("/archive", ImportArchive);
    }

    private static async Task<IResult> GetCurrent(UvwebScraper scraper, AppDbContext db, CancellationToken ct)
    {
        var current = await scraper.GetCurrentAsync(ct);
        var imported = await ImportedIds(db, ct);
        return TypedResults.Ok(current.Select(c => c with { IsImported = imported.Contains(c.MsId) }));
    }

    private static async Task<IResult> ImportCurrent(int msId, int uvId, int ranglisteId, UvwebImporter importer, CancellationToken ct) =>
        TypedResults.Ok(await importer.ImportAsync(msId, uvId, ranglisteId, ct));

    private static async Task<IResult> GetAssociations(UvwebScraper scraper, CancellationToken ct) =>
        TypedResults.Ok(await scraper.GetAssociationsAsync(ct));

    private static async Task<IResult> GetArchive(int uvId, UvwebScraper scraper, AppDbContext db, CancellationToken ct)
    {
        var archive = await scraper.GetArchiveAsync(uvId, ct);
        var imported = await ImportedIds(db, ct);
        return TypedResults.Ok(archive.Select(a => a with { IsImported = imported.Contains(a.MsId) }));
    }

    private static async Task<IResult> ImportArchive(ImportArchiveRequest request, UvwebImporter importer, CancellationToken ct) =>
        TypedResults.Ok(await importer.ImportArchiveAsync(request.MsId, request.UvId, request.PdfUrl, ct));

    private static async Task<HashSet<int>> ImportedIds(AppDbContext db, CancellationToken ct) =>
        (await db.Championships.Where(c => c.ExternalId != null).Select(c => c.ExternalId!.Value).ToListAsync(ct)).ToHashSet();
}
