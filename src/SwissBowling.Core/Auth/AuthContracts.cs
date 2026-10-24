using System.ComponentModel.DataAnnotations;

namespace SwissBowling.Core.Auth;

public static class AuthRoutes
{
    public const string Login = "api/auth/login";
    public const string Users = "api/users";

    public static string User(Guid id) => $"{Users}/{id}";

    public static string Password(Guid id) => $"{Users}/{id}/password";
}

public static class Roles
{
    /// <summary>Vollzugriff auf Stammdaten, Meisterschaften und Benutzer.</summary>
    public const string Admin = "Admin";

    /// <summary>Darf Resultate erfassen und Runden abschliessen.</summary>
    public const string Scorer = "Erfasser";

    public static readonly string[] All = [Admin, Scorer];
}

public sealed record LoginRequest([property: Required, EmailAddress] string Email, [property: Required] string Password);

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, UserDto User);

public sealed record UserDto(Guid Id, string Email, IReadOnlyList<string> Roles);

public sealed record CreateUserRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(8)] string Password,
    [property: Required, MinLength(1)] IReadOnlyList<string> Roles);

public sealed record SetPasswordRequest([property: Required, MinLength(8)] string NewPassword);
