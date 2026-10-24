using Microsoft.EntityFrameworkCore;
using SwissBowling.Core.Seasons;
using SwissBowling.Server.Auth;
using SwissBowling.Server.Data;
using SwissBowling.Server.Infrastructure;

namespace SwissBowling.Server.Features.Seasons;

public static class SeasonEndpoints
{
    public static void MapSeasons(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(SeasonRoutes.Base).WithTags("Saisons");

        group.MapGet("/", GetAll);
        group.MapPost("/", Create).RequireAuthorization(Policies.Admin);
        group.MapPut("/{id:guid}", Update).RequireAuthorization(Policies.Admin);
        group.MapDelete("/{id:guid}", Delete).RequireAuthorization(Policies.Admin);
    }

    private static async Task<IResult> GetAll(AppDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Seasons.AsNoTracking()
            .OrderByDescending(s => s.StartDate)
            .Select(s => new SeasonDto(s.Id, s.Name, s.StartDate, s.EndDate, s.Championships.Count))
            .ToListAsync(ct));

    private static async Task<IResult> Create(SaveSeasonRequest request, AppDbContext db, CancellationToken ct)
    {
        if (Validate(request) is { } problem)
        {
            return problem;
        }

        var season = new Season { Name = request.Name.Trim(), StartDate = request.StartDate, EndDate = request.EndDate };
        db.Seasons.Add(season);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created(SeasonRoutes.ById(season.Id), new SeasonDto(season.Id, season.Name, season.StartDate, season.EndDate, 0));
    }

    private static async Task<IResult> Update(Guid id, SaveSeasonRequest request, AppDbContext db, CancellationToken ct)
    {
        if (Validate(request) is { } problem)
        {
            return problem;
        }

        var season = await db.Seasons.Include(s => s.Championships).FirstOrDefaultAsync(s => s.Id == id, ct);
        if (season is null)
        {
            return Problems.NotFound("Saison");
        }

        season.Name = request.Name.Trim();
        season.StartDate = request.StartDate;
        season.EndDate = request.EndDate;
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(new SeasonDto(season.Id, season.Name, season.StartDate, season.EndDate, season.Championships.Count));
    }

    private static async Task<IResult> Delete(Guid id, AppDbContext db, CancellationToken ct)
    {
        var season = await db.Seasons.FindAsync([id], ct);
        if (season is null)
        {
            return Problems.NotFound("Saison");
        }

        if (await db.Championships.AnyAsync(c => c.SeasonId == id, ct))
        {
            return Problems.Conflict("Die Saison enthält Meisterschaften und kann nicht gelöscht werden.");
        }

        db.Seasons.Remove(season);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static IResult? Validate(SaveSeasonRequest request) =>
        request.EndDate <= request.StartDate ? Problems.Invalid("Das Saisonende muss nach dem Saisonstart liegen.") : null;
}
