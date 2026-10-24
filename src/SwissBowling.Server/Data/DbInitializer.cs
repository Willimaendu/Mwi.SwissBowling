using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SwissBowling.Core.Auth;
using SwissBowling.Server.Auth;

namespace SwissBowling.Server.Data;

/// <summary>Legt beim Start Schema, Rollen und den initialen Admin an.</summary>
public sealed class DbInitializer(IServiceProvider services, IConfiguration configuration, ILogger<DbInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (db.Database.GetMigrations().Any())
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            logger.LogWarning("Keine Migrationen gefunden – Schema wird mit EnsureCreated angelegt.");
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var email = configuration["Seed:AdminEmail"] ?? "admin@swissbowling.local";
        var password = configuration["Seed:AdminPassword"];
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        if (string.IsNullOrWhiteSpace(password) || await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var admin = new AppUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(admin, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.Admin);
            logger.LogInformation("Admin-Benutzer {Email} angelegt.", email);
        }
        else
        {
            logger.LogError("Admin konnte nicht angelegt werden: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
