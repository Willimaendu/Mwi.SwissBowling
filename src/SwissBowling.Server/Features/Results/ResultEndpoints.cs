using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SwissBowling.Core.Auth;
using SwissBowling.Core.Championships;
using SwissBowling.Core.Results;
using SwissBowling.Server.Auth;
using SwissBowling.Server.Data;
using SwissBowling.Server.Features.Championships;
using SwissBowling.Server.Infrastructure;

namespace SwissBowling.Server.Features.Results;

public static class ResultEndpoints
{
    public static void MapResults(this IEndpointRouteBuilder app)
    {
        app.MapGet(ChampionshipRoutes.RoundBase + "/{roundId:guid}/results", GetForRound).WithTags("Resultate");
        app.MapPut(ChampionshipRoutes.RoundBase + "/{roundId:guid}/results", Save).WithTags("Resultate").RequireAuthorization(Policies.Scorer);
        app.MapDelete("api/results/{id:guid}", Delete).WithTags("Resultate").RequireAuthorization(Policies.Scorer);
    }

    private static async Task<IResult> GetForRound(Guid roundId, AppDbContext db, CancellationToken ct)
    {
        var round = await LoadRound(db, roundId, tracking: false, ct);
        return round is null ? Problems.NotFound("Runde") : TypedResults.Ok(ToDetail(round));
    }

    /// <summary>Speichert alle übermittelten Resultate einer Runde (Upsert pro Kegler).</summary>
    private static async Task<IResult> Save(Guid roundId, SaveResultsRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var round = await LoadRound(db, roundId, tracking: true, ct);
        if (round is null)
        {
            return Problems.NotFound("Runde");
        }

        if (round.IsCompleted && !user.IsInRole(Roles.Admin))
        {
            return Problems.Forbidden("Die Runde ist abgeschlossen. Nur Administratoren dürfen Resultate noch ändern.");
        }

        var maxPins = round.Championship.MaxPinsPerRound;
        if (request.Results.Any(r => r.Pins > maxPins))
        {
            return Problems.Invalid($"Ein Resultat darf höchstens {maxPins} Holz betragen.");
        }

        if (request.Results.Select(r => r.PlayerId).Distinct().Count() != request.Results.Count)
        {
            return Problems.Invalid("Ein Kegler darf pro Runde nur ein Resultat haben.");
        }

        var playerIds = request.Results.Select(r => r.PlayerId).ToList();
        var players = await db.Players.Include(p => p.Club).Where(p => playerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        if (players.Count != playerIds.Count)
        {
            return Problems.Invalid("Mindestens ein Kegler existiert nicht.");
        }

        foreach (var input in request.Results)
        {
            var existing = round.Results.FirstOrDefault(r => r.PlayerId == input.PlayerId);
            if (existing is null)
            {
                round.Results.Add(new Result { Round = round, Player = players[input.PlayerId], Pins = input.Pins });
            }
            else
            {
                existing.Pins = input.Pins;
            }
        }

        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(ToDetail(round));
    }

    private static async Task<IResult> Delete(Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var result = await db.Results.Include(r => r.Round).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (result is null)
        {
            return Problems.NotFound("Resultat");
        }

        if (result.Round.IsCompleted && !user.IsInRole(Roles.Admin))
        {
            return Problems.Forbidden("Die Runde ist abgeschlossen. Nur Administratoren dürfen Resultate löschen.");
        }

        db.Results.Remove(result);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static Task<Round?> LoadRound(AppDbContext db, Guid roundId, bool tracking, CancellationToken ct)
    {
        var query = db.Rounds
            .Include(r => r.Championship).ThenInclude(c => c.Season)
            .Include(r => r.Championship).ThenInclude(c => c.Rounds).ThenInclude(x => x.Results)
            .Include(r => r.Results).ThenInclude(x => x.Player).ThenInclude(p => p.Club);

        return (tracking ? query : query.AsNoTracking()).FirstOrDefaultAsync(r => r.Id == roundId, ct);
    }

    private static RoundDetailDto ToDetail(Round round)
    {
        var participants = round.Championship.Rounds.SelectMany(r => r.Results).Select(r => r.PlayerId).Distinct().Count();
        return new RoundDetailDto(
            round.ToDto(),
            round.Championship.ToDto(participants),
            round.Results.OrderByDescending(r => r.Pins).ThenBy(r => r.Player.LastName).Select(r => r.ToDto()).ToList());
    }
}
