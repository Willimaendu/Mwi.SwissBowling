using Microsoft.EntityFrameworkCore;
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
        var players = await db.Players.AsNoTracking()
            .Include(p => p.Club)
            .Where(p => includeInactive == true || p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        return TypedResults.Ok(players.Select(p => p.ToDto()));
    }

    private static async Task<IResult> GetDetail(Guid id, AppDbContext db, CancellationToken ct)
    {
        var player = await db.Players.AsNoTracking().Include(p => p.Club).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (player is null)
        {
            return Problems.NotFound("Kegler");
        }

        var results = await db.Results.AsNoTracking()
            .Where(r => r.PlayerId == id)
            .OrderByDescending(r => r.Championship.StartDate)
            .Select(r => new PlayerResultDto(r.ChampionshipId, r.Championship.Name, r.Date, r.Pins, r.Nines, r.Award))
            .ToListAsync(ct);

        return TypedResults.Ok(new PlayerDetailDto(player.ToDto(), results));
    }

    private static async Task<IResult> Create(SavePlayerRequest request, AppDbContext db, CancellationToken ct)
    {
        var club = await db.Clubs.FindAsync([request.ClubId], ct);
        if (club is null)
        {
            return Problems.Invalid("Der gewählte Klub existiert nicht.");
        }

        if (await LicenseTaken(db, request.LicenseNumber, null, ct))
        {
            return Problems.Conflict("Diese Lizenznummer ist bereits vergeben.");
        }

        var player = new Player { Name = request.Name, Club = club };
        player.Apply(request);
        db.Players.Add(player);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created(PlayerRoutes.ById(player.Id), player.ToDto());
    }

    private static async Task<IResult> Update(Guid id, SavePlayerRequest request, AppDbContext db, CancellationToken ct)
    {
        var player = await db.Players.Include(p => p.Club).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (player is null)
        {
            return Problems.NotFound("Kegler");
        }

        if (await LicenseTaken(db, request.LicenseNumber, id, ct))
        {
            return Problems.Conflict("Diese Lizenznummer ist bereits vergeben.");
        }

        if (player.ClubId != request.ClubId)
        {
            var club = await db.Clubs.FindAsync([request.ClubId], ct);
            if (club is null)
            {
                return Problems.Invalid("Der gewählte Klub existiert nicht.");
            }

            player.Club = club;
        }

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

    private static Task<bool> LicenseTaken(AppDbContext db, string? license, Guid? exceptId, CancellationToken ct)
    {
        var normalized = license?.Trim();
        return string.IsNullOrEmpty(normalized)
            ? Task.FromResult(false)
            : db.Players.AnyAsync(p => p.LicenseNumber == normalized && p.Id != exceptId, ct);
    }
}
