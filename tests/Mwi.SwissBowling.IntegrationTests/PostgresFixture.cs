using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mwi.SwissBowling.Api.Features.Results;
using Mwi.SwissBowling.Api.Features.Setup;
using Mwi.SwissBowling.Api.Persistence;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Core.Persistence;
using Testcontainers.PostgreSql;

namespace Mwi.SwissBowling.IntegrationTests;

[CollectionDefinition("PostgreSQL", DisableParallelization = true)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:18").Build();
    private int year = 2000;
    private int playerNumber = 90000000;
    public TestClock Clock { get; } = new();
    public string ConnectionString => container.GetConnectionString();
    public ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(ConnectionString).Options);
    public ResultRecordingService Recording(ApplicationDbContext database) => new(database, Clock, Options.Create(new EditingLeaseOptions()));
    public async Task InitializeAsync()
    {
        await container.StartAsync();
        await using var database = CreateContext();
        await SchemaDeployment.ApplyAsync(database, typeof(ApplicationDbContext).Assembly, "Mwi.SwissBowling.Api.Persistence.Migrations.");
    }
    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    public async Task<Scenario> CreateScenarioAsync()
    {
        await using var database = CreateContext();
        var season = new Season { Id = Guid.NewGuid(), Year = Interlocked.Increment(ref year) };
        var venue = new Venue { Id = Guid.NewGuid(), Name = "Testanlage", Address = "Fiktive Testadresse" };
        venue.Lanes.Add(new Lane { VenueId = venue.Id, Number = 1 });
        var annual = new AnnualChampionship { Id = Guid.NewGuid(), SeasonId = season.Id, Name = "Testjahr" };
        var championship = new ChampionshipEvent { Id = Guid.NewGuid(), AnnualChampionshipId = annual.Id, Name = "Testanlass", VenueId = venue.Id, StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 2), MaximumThrows = 100 };
        var players = Enumerable.Range(1, 3).Select(index => new Player { Id = Guid.NewGuid(), FirstName = "Beispiel", LastName = $"Test {index}", SfkvNumber = Interlocked.Increment(ref playerNumber).ToString(System.Globalization.CultureInfo.InvariantCulture), Email = "private@example.invalid", DateOfBirth = new DateOnly(1980, 1, 1) }).ToArray();
        database.AddRange(season, venue, annual, championship);
        database.AddRange(players);
        database.AddRange(players.Select(player => new SeasonMember { SeasonId = season.Id, PlayerId = player.Id, Category = PlayerCategory.A }));
        await database.SaveChangesAsync();
        return new Scenario(championship.Id, season.Id, players.Select(player => player.Id).ToArray());
    }
    public static ClaimsPrincipal Actor(string subject) => new(new ClaimsIdentity([new Claim("sub", subject), new Claim("iss", "https://identity.test/realms/swissbowling"), new Claim(ClaimTypes.Role, "SportAdministrator")], "Test"));
}
public sealed record Scenario(Guid EventId, Guid SeasonId, Guid[] PlayerIds);
public sealed class TestClock : TimeProvider
{
    private DateTimeOffset now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(TimeSpan duration) => now += duration;
}
