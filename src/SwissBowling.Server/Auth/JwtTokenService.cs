using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SwissBowling.Server.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "SwissBowling";

    [Required]
    public string Audience { get; set; } = "SwissBowling";

    /// <summary>Mindestens 32 Zeichen (HMAC-SHA256); in Produktion über Secrets setzen.</summary>
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    [Range(5, 1440)]
    public int LifetimeMinutes { get; set; } = 480;

    public SymmetricSecurityKey Key => new(Encoding.UTF8.GetBytes(SigningKey));
}

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) Issue(AppUser user, IEnumerable<string> roles);
}

/// <summary>Erzeugt signierte JWTs mit den Claims <c>sub</c>, <c>email</c> und <c>role</c>.</summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider time) : ITokenService
{
    public (string Token, DateTimeOffset ExpiresAt) Issue(AppUser user, IEnumerable<string> roles)
    {
        var now = time.GetUtcNow();
        var expires = now.AddMinutes(options.Value.LifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty)
        };
        claims.AddRange(roles.Select(r => new Claim("role", r)));

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Value.Issuer,
            Audience = options.Value.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(options.Value.Key, SecurityAlgorithms.HmacSha256)
        });

        return (token, expires);
    }
}
