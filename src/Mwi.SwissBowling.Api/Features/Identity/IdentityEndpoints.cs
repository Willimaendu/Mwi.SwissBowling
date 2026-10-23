using System.Security.Claims;
using HttpResults = Microsoft.AspNetCore.Http.Results;
using Microsoft.EntityFrameworkCore;
using Mwi.SwissBowling.Api.Persistence;
using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Api.Features.Identity;

public static class IdentityEndpoints
{
    public static void MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/identity").RequireAuthorization();
        group.MapGet("/me", async (ClaimsPrincipal user, ApplicationDbContext database, CancellationToken cancellationToken) =>
        {
            var account = await database.Accounts.AsNoTracking().SingleOrDefaultAsync(account => account.Subject == user.Subject() && account.Issuer == user.Issuer(), cancellationToken);
            return HttpResults.Ok(new IdentityProfile(account?.Id, account?.DisplayName ?? user.Identity?.Name ?? "Benutzerkonto", account?.IsApproved == true,
                account?.IsApproved == true && user.HasAdministratorRole(), user.FindAll(ClaimTypes.Role).Select(role => role.Value).Distinct().ToArray()));
        });
        group.MapPost("/register", async (ClaimsPrincipal user, ApplicationDbContext database, TimeProvider clock, CancellationToken cancellationToken) =>
        {
            if (await database.Accounts.AnyAsync(account => account.Subject == user.Subject() && account.Issuer == user.Issuer(), cancellationToken)) return HttpResults.NoContent();
            var account = new ApplicationAccount
            {
                Id = Guid.NewGuid(), Subject = user.Subject(), Issuer = user.Issuer(),
                DisplayName = (user.Identity?.Name ?? "Benutzerkonto")[..Math.Min((user.Identity?.Name ?? "Benutzerkonto").Length, 200)],
                RegisteredAt = clock.GetUtcNow()
            };
            database.Accounts.Add(account);
            database.Audit("AccountRegistered", account.Id.ToString(), user.Issuer(), user.Subject(), clock.GetUtcNow());
            await database.SaveChangesAsync(cancellationToken);
            return HttpResults.Created("/api/identity/me", new CreatedId(account.Id));
        });
        group.MapGet("/pending", async (ApplicationDbContext database, CancellationToken cancellationToken) =>
            await database.Accounts.AsNoTracking().Where(account => !account.IsApproved).OrderBy(account => account.RegisteredAt)
                .Select(account => new PendingAccount(account.Id, account.DisplayName, account.RegisteredAt)).ToArrayAsync(cancellationToken)).RequireAuthorization(ApiIdentity.Administrator);
        group.MapPost("/{id:guid}/approve", ApproveAsync).RequireAuthorization(ApiIdentity.Administrator);
    }

    private static async Task<IResult> ApproveAsync(Guid id, ApproveAccountRequest input, ClaimsPrincipal user, ApplicationDbContext database, TimeProvider clock, CancellationToken cancellationToken)
    {
        if (!input.IdentityVerified) return HttpResults.ValidationProblem(new Dictionary<string, string[]> { ["identityVerified"] = ["Die Identität und gegebenenfalls die Spielerzuordnung müssen geprüft sein."] });
        var account = await database.Accounts.FindAsync([id], cancellationToken);
        if (account is null) return HttpResults.NotFound();
        if (account.IsApproved) return HttpResults.Conflict(new { message = "Das Konto ist bereits freigegeben." });
        if (input.PlayerId is not null && !await database.Set<Features.Setup.Player>().AnyAsync(player => player.Id == input.PlayerId, cancellationToken)) return HttpResults.BadRequest();
        account.IsApproved = true;
        account.PlayerId = input.PlayerId;
        database.Audit("AccountApproved", id.ToString(), user.Issuer(), user.Subject(), clock.GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);
        return HttpResults.NoContent();
    }
}
