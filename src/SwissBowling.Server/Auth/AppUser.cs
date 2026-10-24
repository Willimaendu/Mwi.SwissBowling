using Microsoft.AspNetCore.Identity;

namespace SwissBowling.Server.Auth;

public class AppUser : IdentityUser<Guid>;

public static class Policies
{
    public const string Admin = "Admin";
    public const string Scorer = "Erfasser";
}
