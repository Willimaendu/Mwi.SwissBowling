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
        group.MapPost("/", Create).RequireAuthorization(Policies.Admin);
        group.MapPut("/{id:guid}", Update).RequireAuthorization(Policies.Admin);
        group.MapDelete("/{id:guid}", Delete).RequireAuthorization(Policies.Admin);
    }

    private static async Task<IResult> GetAll(AppDbContext db, CancellationToken ct)
    {
        var items = await db.Associations.AsNoTracking().Include(a => a.Clubs).OrderBy(a => a.Name).ToListAsync(ct);
        return TypedResults.Ok(items.Select(a => a.ToDto()));
    }

    private static async Task<IResult> Create(SaveAssociationRequest request, AppDbContext db, CancellationToken ct)
    {
        var association = new Association { Name = request.Name.Trim() };
        db.Associations.Add(association);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created(AssociationRoutes.ById(association.Id), association.ToDto());
    }

    private static async Task<IResult> Update(Guid id, SaveAssociationRequest request, AppDbContext db, CancellationToken ct)
    {
        var association = await db.Associations.Include(a => a.Clubs).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (association is null)
        {
            return Problems.NotFound("Unterverband");
        }

        association.Name = request.Name.Trim();
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

        if (await db.Clubs.AnyAsync(c => c.AssociationId == id, ct) || await db.Championships.AnyAsync(c => c.AssociationId == id, ct))
        {
            return Problems.Conflict("Der Unterverband hat Klubs oder Meisterschaften und kann nicht gelöscht werden.");
        }

        db.Associations.Remove(association);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
