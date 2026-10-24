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
        var clubs = await db.Clubs.AsNoTracking().Include(c => c.Association).Include(c => c.Players).OrderBy(c => c.Name).ToListAsync(ct);
        return TypedResults.Ok(clubs.Select(c => c.ToDto()));
    }

    private static async Task<IResult> Create(SaveClubRequest request, AppDbContext db, CancellationToken ct)
    {
        var association = await db.Associations.FindAsync([request.AssociationId], ct);
        if (association is null)
        {
            return Problems.Invalid("Der gewählte Unterverband existiert nicht.");
        }

        var club = new Club { Name = request.Name.Trim(), City = request.City?.Trim(), Association = association };
        db.Clubs.Add(club);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created(ClubRoutes.ById(club.Id), club.ToDto());
    }

    private static async Task<IResult> Update(Guid id, SaveClubRequest request, AppDbContext db, CancellationToken ct)
    {
        var club = await db.Clubs.Include(c => c.Association).Include(c => c.Players).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (club is null)
        {
            return Problems.NotFound("Klub");
        }

        if (club.AssociationId != request.AssociationId)
        {
            var association = await db.Associations.FindAsync([request.AssociationId], ct);
            if (association is null)
            {
                return Problems.Invalid("Der gewählte Unterverband existiert nicht.");
            }

            club.Association = association;
        }

        club.Name = request.Name.Trim();
        club.City = request.City?.Trim();
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(club.ToDto());
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
}
