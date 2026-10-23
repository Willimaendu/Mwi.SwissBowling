using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Program = Mwi.SwissBowling.Api.Program;

namespace Mwi.SwissBowling.IntegrationTests;

public sealed class ApiFactory(PostgresFixture fixture) : WebApplicationFactory<Program>
{
    public const string Issuer = "https://identity.test/realms/swissbowling";
    private readonly RSA rsa = RSA.Create(2048);
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:application", fixture.ConnectionString);
        builder.UseSetting("Authentication:Authority", Issuer);
        builder.UseSetting("Authentication:Audience", "swissbowling-api");
        builder.ConfigureTestServices(services => services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var key = new RsaSecurityKey(rsa) { KeyId = "integration-key" };
            options.Configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
            options.Configuration.SigningKeys.Add(key);
            options.TokenValidationParameters.ValidIssuer = Issuer;
            options.TokenValidationParameters.ValidAudience = "swissbowling-api";
            options.TokenValidationParameters.IssuerSigningKey = key;
        }));
    }
    public string Token(string subject, string[] roles, string audience = "swissbowling-api") => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
    {
        Issuer = Issuer,
        Audience = audience,
        Expires = DateTime.UtcNow.AddMinutes(5),
        SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa) { KeyId = "integration-key" }, SecurityAlgorithms.RsaSha256),
        Claims = new Dictionary<string, object> { ["sub"] = subject, ["name"] = "Testkonto", ["realm_access"] = new Dictionary<string, object> { ["roles"] = roles } }
    });
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        rsa.Dispose();
    }
}
