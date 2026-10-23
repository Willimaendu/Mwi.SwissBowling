namespace Mwi.SwissBowling.Api.Features.Identity;

public static class IdentityEndpoints
{
    public static void MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/identity").RequireAuthorization();
        group.MapGet("/me", IdentityOperations.ReadProfileAsync);
        group.MapPost("/register", IdentityOperations.RegisterAsync);
        group.MapGet("/pending", IdentityOperations.PendingAsync).RequireAuthorization(ApiIdentity.Administrator);
        group.MapPost("/{id:guid}/approve", IdentityOperations.ApproveAsync).RequireAuthorization(ApiIdentity.Administrator);
    }
}
