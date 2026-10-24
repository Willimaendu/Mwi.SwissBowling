using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SwissBowling.Server.Data;

/// <summary>Nur für die EF-Tools (dotnet ef migrations add …); zur Laufzeit liefert Aspire die Verbindung.</summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("SWISSBOWLING_DESIGN_CONNECTION") ?? "Host=localhost;Database=swissbowling;Username=postgres;Password=postgres")
            .Options);
}
