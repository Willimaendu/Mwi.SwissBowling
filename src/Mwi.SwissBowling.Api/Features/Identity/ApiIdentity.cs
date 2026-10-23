using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Mwi.SwissBowling.Api.Persistence;
using Mwi.SwissBowling.Core.Authentication;

namespace Mwi.SwissBowling.Api.Features.Identity;

public static class ApiIdentity
{
    public const string Administrator = "ApprovedAdministrator";
    public static void AddApiIdentity(this WebApplicationBuilder builder)
    {
        var authority = builder.Configuration["Authentication:Authority"] ?? throw new InvalidOperationException("Authentication:Authority is required.");
        if (!Uri.TryCreate(authority, UriKind.Absolute, out var uri) || (!builder.Environment.IsDevelopment() && uri.Scheme != "https"))
            throw new InvalidOperationException("A valid HTTPS authentication authority is required outside development.");
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = builder.Configuration["Authentication:Audience"] ?? "swissbowling-api";
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
                NameClaimType = "name", RoleClaimType = ClaimTypes.Role, ClockSkew = TimeSpan.FromSeconds(30)
            };
        });
        builder.Services.AddTransient<IClaimsTransformation, KeycloakRoles>();
        builder.Services.AddScoped<IAuthorizationHandler, ApprovedAdministratorHandler>();
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.AddPolicy(Administrator, policy => policy.RequireAuthenticatedUser().AddRequirements(new ApprovedAdministratorRequirement()));
        });
    }

    public static string Subject(this ClaimsPrincipal user) => user.FindFirstValue("sub") ?? throw new InvalidOperationException("Authenticated subject is missing.");
    public static string Issuer(this ClaimsPrincipal user) => user.FindFirstValue("iss") ?? throw new InvalidOperationException("Authenticated issuer is missing.");
    public static bool HasAdministratorRole(this ClaimsPrincipal user) => user.IsInRole("SystemAdministrator") || user.IsInRole("SportAdministrator");
}

public sealed class ApprovedAdministratorRequirement : IAuthorizationRequirement;

public sealed class ApprovedAdministratorHandler(IDbContextFactory<ApplicationDbContext> factory) : AuthorizationHandler<ApprovedAdministratorRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ApprovedAdministratorRequirement requirement)
    {
        var subject = context.User.FindFirstValue("sub");
        var issuer = context.User.FindFirstValue("iss");
        if (subject is null || issuer is null || !context.User.HasAdministratorRole()) return;
        await using var database = await factory.CreateDbContextAsync();
        if (await database.Accounts.AnyAsync(account => account.Subject == subject && account.Issuer == issuer && account.IsApproved))
            context.Succeed(requirement);
    }
}
