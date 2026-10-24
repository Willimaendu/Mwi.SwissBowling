using Microsoft.EntityFrameworkCore;
using SwissBowling.Core.Championships;
using SwissBowling.Core.Players;
using SwissBowling.Server.Auth;
using SwissBowling.Server.Data;
using SwissBowling.Server.Infrastructure;

namespace SwissBowling.Server.Features.Championships;

public static class ChampionshipEndpoints
{
    public static void MapChampionships(this IEndpointRouteBuilder app)
    {
        var championships = app.MapGroup(ChampionshipRoutes.Base).WithTags("Meisterschaften");
        championships.MapGet("/", GetAll);
        championships.MapGet("/{id:guid}", GetDetail);
        championships.MapGet("/{id:guid}/ranking", GetRanking);
        championships.MapPost("/", Create).RequireAuthorization(Policies.Admin);
        championships.MapPut("/{id:guid}", Update).RequireAuthorization(Policies.Admin);
        championships.MapDelete("/{id:guid}", Delete).RequireAuthorization(Policies.Admin);
        championships.MapPost("/{id:guid}/rounds", AddRound).RequireAuthorization(Policies.Admin);

        var rounds = app.MapGroup(ChampionshipRoutes.RoundBase).WithTags("Runden");
        rounds.MapPut("/{id:guid}", UpdateRound).RequireAuthorization(Policies.Admin);
        rounds.MapPut("/{id:guid}/completed", SetRoundCompleted).RequireAuthorization(Policies.Scorer);
        rounds.MapDelete("/{id:guid}", DeleteRound).RequireAuthorization(Policies.Admin);
    }

    private static async Task<IResult> GetAll(Guid? seasonId, AppDbContext db, CancellationToken ct)
    {
        var items = await Query(db)
            .Where(c => seasonId == null || c.SeasonId == seasonId)
            .OrderByDescending(c => c.Season.StartDate).ThenBy(c => c.Name)
            .Select(c => new { Championship = c, Participants = c.Rounds.SelectMany(r => r.Results).Select(r => r.PlayerId).Distinct().Count() })
            .ToListAsync(ct);

        return TypedResults.Ok(items.Select(x => x.Championship.ToDto(x.Participants)));
    }

    private static async Task<IResult> GetDetail(Guid id, AppDbContext db, CancellationToken ct)
    {
        var item = await Query(db).Include(c => c.Rounds).ThenInclude(r => r.Results)
            .Where(c => c.Id == id)
            .Select(c => new { Championship = c, Participants = c.Rounds.SelectMany(r => r.Results).Select(r => r.PlayerId).Distinct().Count() })
            .FirstOrDefaultAsync(ct);

        return item is null
            ? Problems.NotFound("Meisterschaft")
            : TypedResults.Ok(new ChampionshipDetailDto(
                item.Championship.ToDto(item.Participants),
                item.Championship.Rounds.OrderBy(r => r.Number).Select(r => r.ToDto()).ToList()));
    }

    private static async Task<IResult> GetRanking(Guid id, PlayerCategory? category, AppDbContext db, CancellationToken ct)
    {
        var championship = await Query(db).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (championship is null)
        {
            return Problems.NotFound("Meisterschaft");
        }

        var results = await db.Results.AsNoTracking()
            .Where(r => r.Round.ChampionshipId == id && (category == null || r.Player.Category == category))
            .Select(r => new
            {
                r.PlayerId,
                r.Round.Number,
                r.Pins,
                Name = r.Player.FirstName + " " + r.Player.LastName,
                Club = r.Player.Club.ShortName,
                r.Player.Category
            })
            .ToListAsync(ct);

        var players = results.DistinctBy(r => r.PlayerId).ToDictionary(r => r.PlayerId);
        var entries = RankingCalculator
            .Calculate(results.Select(r => new PlayerRoundResult(r.PlayerId, r.Number, r.Pins)), championship.CountedRounds)
            .Select(e => new RankingEntryDto(
                e.Rank, e.PlayerId, players[e.PlayerId].Name, players[e.PlayerId].Club, players[e.PlayerId].Category,
                e.Total, e.Average, e.Best, e.RoundsPlayed, e.PinsByRound, e.DiscardedRoundNumbers))
            .ToList();

        var roundNumbers = championship.Rounds.OrderBy(r => r.Number).Select(r => r.Number).ToList();
        var participants = await db.Results.Where(r => r.Round.ChampionshipId == id).Select(r => r.PlayerId).Distinct().CountAsync(ct);

        return TypedResults.Ok(new RankingDto(championship.ToDto(participants), category, roundNumbers, entries));
    }

    private static async Task<IResult> Create(SaveChampionshipRequest request, AppDbContext db, CancellationToken ct)
    {
        if (Validate(request, existingRounds: 0) is { } problem)
        {
            return problem;
        }

        var season = await db.Seasons.FindAsync([request.SeasonId], ct);
        if (season is null)
        {
            return Problems.Invalid("Die gewählte Saison existiert nicht.");
        }

        var championship = new Championship { Name = request.Name, Season = season };
        championship.Apply(request);
        db.Championships.Add(championship);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created(ChampionshipRoutes.ById(championship.Id), championship.ToDto(0));
    }

    private static async Task<IResult> Update(Guid id, SaveChampionshipRequest request, AppDbContext db, CancellationToken ct)
    {
        var championship = await Query(db, tracking: true).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (championship is null)
        {
            return Problems.NotFound("Meisterschaft");
        }

        if (Validate(request, championship.Rounds.Count) is { } problem)
        {
            return problem;
        }

        championship.Apply(request);
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(championship.ToDto(await CountParticipants(db, id, ct)));
    }

    private static async Task<IResult> Delete(Guid id, AppDbContext db, CancellationToken ct)
    {
        var championship = await db.Championships.FindAsync([id], ct);
        if (championship is null)
        {
            return Problems.NotFound("Meisterschaft");
        }

        if (await db.Results.AnyAsync(r => r.Round.ChampionshipId == id, ct))
        {
            return Problems.Conflict("Die Meisterschaft enthält Resultate und kann nicht gelöscht werden.");
        }

        db.Championships.Remove(championship);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> AddRound(Guid id, SaveRoundRequest request, AppDbContext db, CancellationToken ct)
    {
        var championship = await Query(db, tracking: true).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (championship is null)
        {
            return Problems.NotFound("Meisterschaft");
        }

        if (championship.Rounds.Count >= championship.NumberOfRounds)
        {
            return Problems.Conflict($"Die Meisterschaft hat bereits alle {championship.NumberOfRounds} Runden.");
        }

        var round = new Round
        {
            Championship = championship,
            Number = championship.Rounds.Count == 0 ? 1 : championship.Rounds.Max(r => r.Number) + 1,
            Date = request.Date,
            Venue = request.Venue.Trim()
        };
        championship.Rounds.Add(round);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created(ChampionshipRoutes.Round(round.Id), round.ToDto());
    }

    private static async Task<IResult> UpdateRound(Guid id, SaveRoundRequest request, AppDbContext db, CancellationToken ct)
    {
        var round = await LoadRound(db, id, ct);
        if (round is null)
        {
            return Problems.NotFound("Runde");
        }

        round.Date = request.Date;
        round.Venue = request.Venue.Trim();
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(round.ToDto());
    }

    private static async Task<IResult> SetRoundCompleted(Guid id, SetRoundCompletedRequest request, AppDbContext db, CancellationToken ct)
    {
        var round = await LoadRound(db, id, ct);
        if (round is null)
        {
            return Problems.NotFound("Runde");
        }

        round.IsCompleted = request.IsCompleted;
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(round.ToDto());
    }

    private static async Task<IResult> DeleteRound(Guid id, AppDbContext db, CancellationToken ct)
    {
        var round = await LoadRound(db, id, ct);
        if (round is null)
        {
            return Problems.NotFound("Runde");
        }

        if (round.Results.Count > 0)
        {
            return Problems.Conflict("Die Runde enthält Resultate und kann nicht gelöscht werden.");
        }

        if (await db.Rounds.AnyAsync(r => r.ChampionshipId == round.ChampionshipId && r.Number > round.Number, ct))
        {
            return Problems.Conflict("Nur die letzte Runde einer Meisterschaft kann gelöscht werden.");
        }

        db.Rounds.Remove(round);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static IQueryable<Championship> Query(AppDbContext db, bool tracking = false)
    {
        var query = db.Championships.Include(c => c.Season).Include(c => c.Rounds);
        return tracking ? query : query.AsNoTracking();
    }

    private static Task<int> CountParticipants(AppDbContext db, Guid championshipId, CancellationToken ct) =>
        db.Results.Where(r => r.Round.ChampionshipId == championshipId).Select(r => r.PlayerId).Distinct().CountAsync(ct);

    private static Task<Round?> LoadRound(AppDbContext db, Guid id, CancellationToken ct) =>
        db.Rounds.Include(r => r.Championship).Include(r => r.Results).FirstOrDefaultAsync(r => r.Id == id, ct);

    private static IResult? Validate(SaveChampionshipRequest request, int existingRounds)
    {
        if (request.DiscardedRounds >= request.NumberOfRounds)
        {
            return Problems.Invalid("Die Anzahl Streichresultate muss kleiner als die Anzahl Runden sein.");
        }

        return existingRounds > request.NumberOfRounds
            ? Problems.Invalid("Es existieren bereits mehr Runden als die neue Rundenanzahl erlaubt.")
            : null;
    }
}
