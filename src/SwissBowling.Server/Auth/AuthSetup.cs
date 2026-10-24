using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SwissBowling.Core.Auth;
using SwissBowling.Server.Data;

namespace SwissBowling.Server.Auth;

public static class AuthSetup
{
    public static IServiceCollection AddJwtIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<AppUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ITokenService, JwtTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = jwt.Value.Key,
                    RoleClaimType = "role",
                    NameClaimType = "email",
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Admin, p => p.RequireRole(Roles.Admin))
            .AddPolicy(Policies.Scorer, p => p.RequireRole(Roles.Admin, Roles.Scorer));

        return services;
    }
}
