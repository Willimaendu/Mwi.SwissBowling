using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Mwi.SwissBowling.Core.Authentication;
using Mwi.SwissBowling.Core.Persistence;

namespace Mwi.SwissBowling.Web.Authentication;

public static class WebAuthentication
{
    public static void AddWebAuthentication(this WebApplicationBuilder builder)
    {
        var authority = builder.Configuration["Authentication:Authority"] ?? throw new InvalidOperationException("Authentication:Authority is required.");
        if (!Uri.TryCreate(authority, UriKind.Absolute, out var uri) || (!builder.Environment.IsDevelopment() && uri.Scheme != "https"))
            throw new InvalidOperationException("A valid HTTPS authentication authority is required outside development.");
        var certificatePath = builder.Configuration["DataProtection:CertificatePath"] ?? throw new InvalidOperationException("DataProtection:CertificatePath is required.");
        var certificate = X509CertificateLoader.LoadPkcs12FromFile(certificatePath, builder.Configuration["DataProtection:CertificatePassword"], X509KeyStorageFlags.EphemeralKeySet);
        builder.Services.AddSingleton(certificate);
        builder.Services.AddDataProtection().SetApplicationName("Mwi.SwissBowling.Web")
            .PersistKeysToDbContext<WebStateDbContext>().ProtectKeysWithCertificate(certificate);
        builder.Services.AddSingleton<PostgresTicketStore>();
        builder.Services.AddScoped<WebSessionValidator>();
        builder.Services.AddTransient<IClaimsTransformation, KeycloakRoles>();
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "__Host-SwissBowling";
                options.Cookie.Path = "/";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = false;
                options.EventsType = typeof(WebSessionValidator);
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = authority;
                options.ClientId = builder.Configuration["Authentication:ClientId"] ?? "swissbowling-web";
                options.ClientSecret = builder.Configuration["Authentication:ClientSecret"] ?? throw new InvalidOperationException("Authentication:ClientSecret is required.");
                options.ResponseType = "code";
                options.UsePkce = true;
                options.SaveTokens = true;
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                options.MapInboundClaims = false;
                options.Scope.Add("email");
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
                options.Events.OnTokenValidated = context =>
                {
                    context.Properties!.IssuedUtc = DateTimeOffset.UtcNow;
                    context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8);
                    return Task.CompletedTask;
                };
                options.Events.OnRemoteFailure = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    return context.Response.WriteAsync("Die Anmeldung ist derzeit nicht möglich. Bitte versuchen Sie es erneut.");
                };
            });
        builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<PostgresTicketStore>((options, store) => options.SessionStore = store);
        builder.Services.AddAuthorization(options => options.AddPolicy("BffSession", policy =>
            policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme).RequireAuthenticatedUser()));
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "__Host-SwissBowling-Csrf";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });
        builder.Services.AddHttpClient("identity", client => client.Timeout = TimeSpan.FromSeconds(8))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { UseCookies = false, AllowAutoRedirect = false });
    }
}
