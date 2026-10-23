using Microsoft.EntityFrameworkCore;
using Mwi.SwissBowling.Api.Features.Identity;
using Mwi.SwissBowling.Api.Features.Setup;
using Mwi.SwissBowling.Core.Persistence;

namespace Mwi.SwissBowling.Api.Persistence;

public static class DeploymentCommands
{
    public static bool IsRequested(string[] args) => args.Any(argument => argument is "--migrate" or "--seed-demo" or "--bootstrap-admin");

    public static async Task ExecuteAsync(WebApplication app, string[] args)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        if (args.Contains("--migrate", StringComparer.Ordinal))
            await SchemaDeployment.ApplyAsync(database, typeof(ApplicationDbContext).Assembly, "Mwi.SwissBowling.Api.Persistence.Migrations.");
        else if (args.Contains("--seed-demo", StringComparer.Ordinal))
        {
            if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Demo data is allowed only in Development.");
            await DemoSeeder.SeedAsync(database, clock);
        }
        else await BootstrapAsync(app.Configuration, database, args, clock);
    }

    private static async Task BootstrapAsync(IConfiguration configuration, ApplicationDbContext database, string[] args, TimeProvider clock)
    {
        var index = Array.IndexOf(args, "--bootstrap-admin");
        if (index < 0 || index + 1 >= args.Length) throw new InvalidOperationException("Provide the immutable Keycloak subject after --bootstrap-admin.");
        var subject = args[index + 1];
        var issuer = configuration["Authentication:Authority"] ?? throw new InvalidOperationException("Authentication:Authority is required for bootstrap.");
        if (await database.Accounts.AnyAsync(account => account.Subject == subject && account.Issuer == issuer)) throw new InvalidOperationException("Account already exists; use the audited approval workflow.");
        var now = clock.GetUtcNow();
        var account = new ApplicationAccount { Id = Guid.NewGuid(), Issuer = issuer, Subject = subject, DisplayName = "Administration", IsApproved = true, RegisteredAt = now };
        database.Accounts.Add(account);
        database.Audit("AdministratorBootstrapped", account.Id.ToString(), issuer, subject, now);
        await database.SaveChangesAsync();
    }
}
