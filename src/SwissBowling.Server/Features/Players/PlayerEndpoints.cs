using Microsoft.EntityFrameworkCore;
using SwissBowling.Core.Championships;
using SwissBowling.Core.Players;
using SwissBowling.Server.Auth;
using SwissBowling.Server.Data;
using SwissBowling.Server.Infrastructure;

namespace SwissBowling.Server.Features.Players;

public static class PlayerEndpoints
{
    public static void MapPlayers(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(PlayerRoutes.Base).WithTags("Kegler");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetDetail);
        group.MapPost("/", Create).RequireAuthorization(Policies.Admin);
        group.MapPut("/{id:guid}", Update).RequireAuthorization(Policies.Admin);
        group.MapDelete("/{id:guid}", Delete).RequireAuthorization(Policies.Admin);
    }

    private static async Task<IResult> GetAll(bool? includeInactive, AppDbContext db, CancellationToken ct)
    {
        var players = await Query(db)
            .Where(p => includeInactive == true || p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        return TypedResults.Ok(players.Select(p => p.ToDto()));
    }

    private static async Task<IResult> GetDetail(Guid id, AppDbContext db, CancellationToken ct)
    {
        var player = await Query(db).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (player is null)
        {
            return Problems.NotFound("Kegler");
        }

        var own = await db.Results.AsNoTracking()
            .Where(r => r.PlayerId == id)
            .OrderByDescending(r => r.Championship.StartDate)
            .Select(r => new { r.ChampionshipId, r.Championship.Name, r.Discipline, r.Date, r.Pins, r.Nines, r.Award })
            .ToListAsync(ct);

        // Platzierung innerhalb von Disziplin und Kategorie: alle entsprechenden Resultate dieser Meisterschaften rangieren.
        var championshipIds = own.Select(r => r.ChampionshipId).ToList();
        var field = await db.Results.AsNoTracking()
            .Where(r => championshipIds.Contains(r.ChampionshipId) && r.Player.Category == player.Category)
            .Select(r => new { r.ChampionshipId, r.Discipline, r.PlayerId, r.Pins, r.Nines })
            .ToListAsync(ct);

        var rankings = field.GroupBy(r => (r.ChampionshipId, r.Discipline)).ToDictionary(
            g => g.Key,
            g => RankingCalculator.Rank(g, r => r.Pins, r => r.Nines));

        var results = own.Select(r =>
        {
            var ranking = rankings[(r.ChampionshipId, r.Discipline)];
            var rank = ranking.FirstOrDefault(x => x.Item.PlayerId == id).Rank;
            return new PlayerResultDto(r.ChampionshipId, r.Name, r.Discipline, r.Date, r.Pins, r.Nines, r.Award, rank, ranking.Count);
        }).ToList();

        return TypedResults.Ok(new PlayerDetailDto(player.ToDto(), results));
    }

    private static async Task<IResult> Create(SavePlayerRequest request, AppDbContext db, CancellationToken ct)
    {
        var (association, club, problem) = await ResolveAsync(request, db, ct);
        if (problem is not null)
        {
            return problem;
        }

        if (await LicenseTaken(db, request.LicenseNumber, null, ct))
        {
            return Problems.Conflict("Diese Lizenznummer ist bereits vergeben.");
        }

        var player = new Player { Name = request.Name, Association = association!, Club = club };
        player.Apply(request);
        db.Players.Add(player);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created(PlayerRoutes.ById(player.Id), player.ToDto());
    }

    private static async Task<IResult> Update(Guid id, SavePlayerRequest request, AppDbContext db, CancellationToken ct)
    {
        var player = await Query(db, tracking: true).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (player is null)
        {
            return Problems.NotFound("Kegler");
        }

        if (await LicenseTaken(db, request.LicenseNumber, id, ct))
        {
            return Problems.Conflict("Diese Lizenznummer ist bereits vergeben.");
        }

        var (association, club, problem) = await ResolveAsync(request, db, ct);
        if (problem is not null)
        {
            return problem;
        }

        player.Association = association!;
        player.Club = club;
        player.ClubId = club?.Id;
        player.Apply(request);
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(player.ToDto());
    }

    private static async Task<IResult> Delete(Guid id, AppDbContext db, CancellationToken ct)
    {
        var player = await db.Players.FindAsync([id], ct);
        if (player is null)
        {
            return Problems.NotFound("Kegler");
        }

        if (await db.Results.AnyAsync(r => r.PlayerId == id, ct))
        {
            return Problems.Conflict("Der Kegler hat Resultate und kann nur deaktiviert werden.");
        }

        db.Players.Remove(player);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static IQueryable<Player> Query(AppDbContext db, bool tracking = false)
    {
        var query = db.Players.Include(p => p.Club).Include(p => p.Association);
        return tracking ? query : query.AsNoTracking();
    }

    /// <summary>Unterverband und (optional) Klub des Requests laden; der Klub muss zum Unterverband gehören.</summary>
    private static async Task<(Associations.Association? Association, Clubs.Club? Club, IResult? Problem)> ResolveAsync(SavePlayerRequest request, AppDbContext db, CancellationToken ct)
    {
        var association = await db.Associations.FindAsync([request.AssociationId], ct);
        if (association is null)
        {
            return (null, null, Problems.Invalid("Der gewählte Unterverband existiert nicht."));
        }

        if (request.ClubId is null)
        {
            return (association, null, null);
        }

        var club = await db.Clubs.FindAsync([request.ClubId.Value], ct);
        return club is null || club.AssociationId != association.Id
            ? (null, null, Problems.Invalid("Der gewählte Klub existiert nicht oder gehört nicht zum Unterverband."))
            : (association, club, null);
    }

    private static Task<bool> LicenseTaken(AppDbContext db, string? license, Guid? exceptId, CancellationToken ct)
    {
        var normalized = license?.Trim();
        return string.IsNullOrEmpty(normalized)
            ? Task.FromResult(false)
            : db.Players.AnyAsync(p => p.LicenseNumber == normalized && p.Id != exceptId, ct);
    }
}
