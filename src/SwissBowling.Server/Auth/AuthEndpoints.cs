using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using SwissBowling.Core.Auth;
using SwissBowling.Server.Infrastructure;

namespace SwissBowling.Server.Auth;

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        app.MapPost(AuthRoutes.Login, Login).WithTags("Authentifizierung").AllowAnonymous();

        var users = app.MapGroup(AuthRoutes.Users).WithTags("Benutzer").RequireAuthorization(Policies.Admin);
        users.MapGet("/", GetUsers);
        users.MapPost("/", CreateUser);
        users.MapPut("/{id:guid}/password", SetPassword);
        users.MapDelete("/{id:guid}", DeleteUser);
    }

    private static async Task<IResult> Login(LoginRequest request, UserManager<AppUser> users, ITokenService tokens)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || !await users.CheckPasswordAsync(user, request.Password))
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Anmeldung fehlgeschlagen", detail: "E-Mail oder Passwort ist falsch.");
        }

        var roles = await users.GetRolesAsync(user);
        var (token, expires) = tokens.Issue(user, roles);
        return Results.Ok(new LoginResponse(token, expires, ToDto(user, roles)));
    }

    private static async Task<IResult> GetUsers(UserManager<AppUser> users)
    {
        var list = new List<UserDto>();
        foreach (var user in users.Users.OrderBy(u => u.Email).ToList())
        {
            list.Add(ToDto(user, await users.GetRolesAsync(user)));
        }

        return Results.Ok(list);
    }

    private static async Task<IResult> CreateUser(CreateUserRequest request, UserManager<AppUser> users)
    {
        if (request.Roles.Except(Roles.All).Any())
        {
            return Problems.Invalid("Unbekannte Rolle.");
        }

        var user = new AppUser { UserName = request.Email, Email = request.Email, EmailConfirmed = true };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return Problems.Invalid(Describe(result));
        }

        await users.AddToRolesAsync(user, request.Roles);
        return Results.Created(AuthRoutes.User(user.Id), ToDto(user, request.Roles));
    }

    private static async Task<IResult> SetPassword(Guid id, SetPasswordRequest request, UserManager<AppUser> users)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Problems.NotFound("Benutzer");
        }

        var result = await users.ResetPasswordAsync(user, await users.GeneratePasswordResetTokenAsync(user), request.NewPassword);
        return result.Succeeded ? Results.NoContent() : Problems.Invalid(Describe(result));
    }

    private static async Task<IResult> DeleteUser(Guid id, ClaimsPrincipal principal, UserManager<AppUser> users)
    {
        if (principal.FindFirstValue(JwtRegisteredClaimNames.Sub) == id.ToString())
        {
            return Problems.Invalid("Sie können sich nicht selbst löschen.");
        }

        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Problems.NotFound("Benutzer");
        }

        await users.DeleteAsync(user);
        return Results.NoContent();
    }

    private static UserDto ToDto(AppUser user, IEnumerable<string> roles) => new(user.Id, user.Email ?? string.Empty, roles.ToList());

    private static string Describe(IdentityResult result) => string.Join(" ", result.Errors.Select(e => e.Description));
}
