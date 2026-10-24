using Microsoft.EntityFrameworkCore;
using SwissBowling.Core.Clubs;
using SwissBowling.Server.Auth;
using SwissBowling.Server.Data;
using SwissBowling.Server.Infrastructure;

namespace SwissBowling.Server.Features.Clubs;

public static class ClubEndpoints
{
    public static void MapClubs(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ClubRoutes.Base).WithTags("Klubs");

        group.MapGet("/", GetAll);
        group.MapPost("/", Create).RequireAuthorization(Policies.Admin);
        group.MapPut("/{id:guid}", Update).RequireAuthorization(Policies.Admin);
        group.MapDelete("/{id:guid}", Delete).RequireAuthorization(Policies.Admin);
    }

    private static async Task<IResult> GetAll(AppDbContext db, CancellationToken ct)
    {
        var clubs = await db.Clubs.AsNoTracking().Include(c => c.Players).OrderBy(c => c.Name).ToListAsync(ct);
        return TypedResults.Ok(clubs.Select(ToDto));
    }

    private static async Task<IResult> Create(SaveClubRequest request, AppDbContext db, CancellationToken ct)
    {
        if (await ShortNameTaken(db, request.ShortName, null, ct))
        {
            return Problems.Conflict("Dieses Kürzel wird bereits verwendet.");
        }

        var club = new Club { Name = request.Name.Trim(), ShortName = Normalize(request.ShortName), City = request.City?.Trim() };
        db.Clubs.Add(club);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created(ClubRoutes.ById(club.Id), ToDto(club));
    }

    private static async Task<IResult> Update(Guid id, SaveClubRequest request, AppDbContext db, CancellationToken ct)
    {
        var club = await db.Clubs.Include(c => c.Players).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (club is null)
        {
            return Problems.NotFound("Klub");
        }

        if (await ShortNameTaken(db, request.ShortName, id, ct))
        {
            return Problems.Conflict("Dieses Kürzel wird bereits verwendet.");
        }

        club.Name = request.Name.Trim();
        club.ShortName = Normalize(request.ShortName);
        club.City = request.City?.Trim();
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(ToDto(club));
    }

    private static async Task<IResult> Delete(Guid id, AppDbContext db, CancellationToken ct)
    {
        var club = await db.Clubs.FindAsync([id], ct);
        if (club is null)
        {
            return Problems.NotFound("Klub");
        }

        if (await db.Players.AnyAsync(p => p.ClubId == id, ct))
        {
            return Problems.Conflict("Der Klub hat noch Kegler und kann nicht gelöscht werden.");
        }

        db.Clubs.Remove(club);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static string Normalize(string shortName) => shortName.Trim().ToUpperInvariant();

    private static Task<bool> ShortNameTaken(AppDbContext db, string shortName, Guid? exceptId, CancellationToken ct)
    {
        var normalized = Normalize(shortName);
        return db.Clubs.AnyAsync(c => c.ShortName == normalized && c.Id != exceptId, ct);
    }

    private static ClubDto ToDto(Club c) => new(c.Id, c.Name, c.ShortName, c.City, c.Players.Count(p => p.IsActive));
}
