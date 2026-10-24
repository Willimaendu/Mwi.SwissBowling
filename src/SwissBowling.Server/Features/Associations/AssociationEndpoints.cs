using Microsoft.EntityFrameworkCore;
using SwissBowling.Core.Associations;
using SwissBowling.Server.Auth;
using SwissBowling.Server.Data;
using SwissBowling.Server.Infrastructure;

namespace SwissBowling.Server.Features.Associations;

public static class AssociationEndpoints
{
    public static void MapAssociations(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(AssociationRoutes.Base).WithTags("Unterverbände");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetDetail);
        group.MapPost("/", Create).RequireAuthorization(Policies.Admin);
        group.MapPut("/{id:guid}", Update).RequireAuthorization(Policies.Admin);
        group.MapDelete("/{id:guid}", Delete).RequireAuthorization(Policies.Admin);
    }

    private static async Task<IResult> GetAll(AppDbContext db, CancellationToken ct)
    {
        var items = await Query(db).OrderBy(a => a.Name).ToListAsync(ct);
        return TypedResults.Ok(items.Select(a => a.ToDto()));
    }

    private static async Task<IResult> GetDetail(Guid id, AppDbContext db, CancellationToken ct)
    {
        var association = await Query(db)
            .Include(a => a.Board)
            .Include(a => a.Clubs).ThenInclude(c => c.Players)
            .Include(a => a.Venues)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (association is null)
        {
            return Problems.NotFound("Unterverband");
        }

        return TypedResults.Ok(new AssociationDetailDto(
            association.ToDto(),
            association.Board.OrderBy(b => b.Function).Select(b => b.ToDto()).ToList(),
            association.Clubs.OrderBy(c => c.Name).Select(c => c.ToDto()).ToList(),
            association.Venues.OrderBy(v => v.Zip).Select(v => v.ToDto()).ToList()));
    }

    private static async Task<IResult> Create(SaveAssociationRequest request, AppDbContext db, CancellationToken ct)
    {
        var association = new Association { Name = request.Name.Trim(), ShortCode = request.ShortCode?.Trim(), Website = request.Website?.Trim() };
        db.Associations.Add(association);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created(AssociationRoutes.ById(association.Id), association.ToDto());
    }

    private static async Task<IResult> Update(Guid id, SaveAssociationRequest request, AppDbContext db, CancellationToken ct)
    {
        var association = await Query(db, tracking: true).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (association is null)
        {
            return Problems.NotFound("Unterverband");
        }

        association.Name = request.Name.Trim();
        association.ShortCode = request.ShortCode?.Trim();
        association.Website = request.Website?.Trim();
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(association.ToDto());
    }

    private static async Task<IResult> Delete(Guid id, AppDbContext db, CancellationToken ct)
    {
        var association = await db.Associations.FindAsync([id], ct);
        if (association is null)
        {
            return Problems.NotFound("Unterverband");
        }

        if (await db.Clubs.AnyAsync(c => c.AssociationId == id, ct) || await db.Players.AnyAsync(p => p.AssociationId == id, ct) || await db.Championships.AnyAsync(c => c.AssociationId == id, ct))
        {
            return Problems.Conflict("Der Unterverband hat Klubs, Kegler oder Meisterschaften und kann nicht gelöscht werden.");
        }

        db.Associations.Remove(association);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static IQueryable<Association> Query(AppDbContext db, bool tracking = false)
    {
        var query = db.Associations.Include(a => a.Clubs).Include(a => a.Players);
        return tracking ? query : query.AsNoTracking();
    }
}
