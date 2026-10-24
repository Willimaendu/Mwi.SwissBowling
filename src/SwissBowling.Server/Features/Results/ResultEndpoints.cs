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
        var pattern = ChampionshipRoutes.Base + "/{championshipId:guid}/results";
        app.MapGet(pattern, GetForChampionship).WithTags("Resultate");
        app.MapPut(pattern, Save).WithTags("Resultate").RequireAuthorization(Policies.Scorer);
        app.MapDelete("api/results/{id:guid}", Delete).WithTags("Resultate").RequireAuthorization(Policies.Scorer);
    }

    private static async Task<IResult> GetForChampionship(Guid championshipId, AppDbContext db, CancellationToken ct)
    {
        var results = await db.Results.AsNoTracking()
            .Include(r => r.Player).ThenInclude(p => p.Club)
            .Where(r => r.ChampionshipId == championshipId)
            .OrderByDescending(r => r.Pins).ThenByDescending(r => r.Nines)
            .ToListAsync(ct);

        return TypedResults.Ok(results.Select(r => r.ToDto()));
    }

    /// <summary>Speichert alle übermittelten Resultate (Upsert pro Kegler).</summary>
    private static async Task<IResult> Save(Guid championshipId, SaveResultsRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var championship = await db.Championships.Include(c => c.Results).FirstOrDefaultAsync(c => c.Id == championshipId, ct);
        if (championship is null)
        {
            return Problems.NotFound("Meisterschaft");
        }

        if (!CanEdit(championship, user))
        {
            return Problems.Forbidden("Die Meisterschaft ist beendet. Nur Administratoren dürfen Resultate noch ändern.");
        }

        if (request.Results.Any(r => r.Pins > championship.MaxPins))
        {
            return Problems.Invalid($"Ein Resultat darf höchstens {championship.MaxPins} Holz betragen.");
        }

        if (request.Results.Select(r => r.PlayerId).Distinct().Count() != request.Results.Count)
        {
            return Problems.Invalid("Ein Kegler darf pro Meisterschaft nur ein Resultat haben.");
        }

        var playerIds = request.Results.Select(r => r.PlayerId).ToList();
        var players = await db.Players.Include(p => p.Club).Where(p => playerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        if (players.Count != playerIds.Count)
        {
            return Problems.Invalid("Mindestens ein Kegler existiert nicht.");
        }

        foreach (var input in request.Results)
        {
            var result = championship.Results.FirstOrDefault(r => r.PlayerId == input.PlayerId);
            if (result is null)
            {
                result = new Result { Championship = championship, Player = players[input.PlayerId] };
                championship.Results.Add(result);
            }

            result.Pins = input.Pins;
            result.Nines = input.Nines;
            result.Award = input.Award;
            result.Date = input.Date;
        }

        await db.SaveChangesAsync(ct);
        return await GetForChampionship(championshipId, db, ct);
    }

    private static async Task<IResult> Delete(Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var result = await db.Results.Include(r => r.Championship).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (result is null)
        {
            return Problems.NotFound("Resultat");
        }

        if (!CanEdit(result.Championship, user))
        {
            return Problems.Forbidden("Die Meisterschaft ist beendet. Nur Administratoren dürfen Resultate löschen.");
        }

        db.Results.Remove(result);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    /// <summary>Erfasser dürfen bis zum Ende der Meisterschaft erfassen, Administratoren immer.</summary>
    private static bool CanEdit(Championship championship, ClaimsPrincipal user) =>
        user.IsInRole(Roles.Admin) || championship.EndDate >= DateOnly.FromDateTime(DateTime.Today);
}
