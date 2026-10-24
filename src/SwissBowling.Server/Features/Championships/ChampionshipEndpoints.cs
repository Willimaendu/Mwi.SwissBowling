using Microsoft.EntityFrameworkCore;
using SwissBowling.Core.Championships;
using SwissBowling.Server.Auth;
using SwissBowling.Server.Data;
using SwissBowling.Server.Infrastructure;

namespace SwissBowling.Server.Features.Championships;

public static class ChampionshipEndpoints
{
    public static void MapChampionships(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ChampionshipRoutes.Base).WithTags("Meisterschaften");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetDetail);
        group.MapPost("/", Create).RequireAuthorization(Policies.Admin);
        group.MapPut("/{id:guid}", Update).RequireAuthorization(Policies.Admin);
        group.MapDelete("/{id:guid}", Delete).RequireAuthorization(Policies.Admin);
    }

    private static async Task<IResult> GetAll(int? year, AppDbContext db, CancellationToken ct)
    {
        var items = await db.Championships.AsNoTracking()
            .Include(c => c.Association).Include(c => c.Results)
            .Where(c => year == null || c.StartDate.Year == year)
            .OrderByDescending(c => c.StartDate).ThenBy(c => c.Name)
            .ToListAsync(ct);

        return TypedResults.Ok(items.Select(c => c.ToDto()));
    }

    private static async Task<IResult> GetDetail(Guid id, AppDbContext db, CancellationToken ct)
    {
        var championship = await db.Championships.AsNoTracking()
            .Include(c => c.Association)
            .Include(c => c.Results).ThenInclude(r => r.Player).ThenInclude(p => p.Club)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (championship is null)
        {
            return Problems.NotFound("Meisterschaft");
        }

        // Rangierung erfolgt pro Kategorie.
        var ranking = championship.Results
            .GroupBy(r => r.Player.Category)
            .OrderBy(g => g.Key)
            .SelectMany(g => RankingCalculator.Rank(g, r => r.Pins, r => r.Nines)
                .Select(x => new RankingEntryDto(x.Rank, x.Item.PlayerId, x.Item.Player.Name, x.Item.Player.Club.Name, g.Key, x.Item.Pins, x.Item.Nines, x.Item.Award)))
            .ToList();

        return TypedResults.Ok(new ChampionshipDetailDto(championship.ToDto(), ranking));
    }

    private static async Task<IResult> Create(SaveChampionshipRequest request, AppDbContext db, CancellationToken ct)
    {
        if (Validate(request) is { } problem)
        {
            return problem;
        }

        var association = await db.Associations.FindAsync([request.AssociationId], ct);
        if (association is null)
        {
            return Problems.Invalid("Der gewählte Unterverband existiert nicht.");
        }

        var championship = new Championship { Name = request.Name, Association = association };
        championship.Apply(request);
        db.Championships.Add(championship);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created(ChampionshipRoutes.ById(championship.Id), championship.ToDto());
    }

    private static async Task<IResult> Update(Guid id, SaveChampionshipRequest request, AppDbContext db, CancellationToken ct)
    {
        if (Validate(request) is { } problem)
        {
            return problem;
        }

        var championship = await db.Championships.Include(c => c.Association).Include(c => c.Results).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (championship is null)
        {
            return Problems.NotFound("Meisterschaft");
        }

        if (championship.AssociationId != request.AssociationId)
        {
            var association = await db.Associations.FindAsync([request.AssociationId], ct);
            if (association is null)
            {
                return Problems.Invalid("Der gewählte Unterverband existiert nicht.");
            }

            championship.Association = association;
        }

        championship.Apply(request);
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(championship.ToDto());
    }

    private static async Task<IResult> Delete(Guid id, AppDbContext db, CancellationToken ct)
    {
        var championship = await db.Championships.FindAsync([id], ct);
        if (championship is null)
        {
            return Problems.NotFound("Meisterschaft");
        }

        if (await db.Results.AnyAsync(r => r.ChampionshipId == id, ct))
        {
            return Problems.Conflict("Die Meisterschaft enthält Resultate und kann nicht gelöscht werden.");
        }

        db.Championships.Remove(championship);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static IResult? Validate(SaveChampionshipRequest request) =>
        request.EndDate < request.StartDate ? Problems.Invalid("Das Enddatum darf nicht vor dem Startdatum liegen.") : null;
}
