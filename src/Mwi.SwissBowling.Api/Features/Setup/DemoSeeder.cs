using Microsoft.EntityFrameworkCore;
using Mwi.SwissBowling.Api.Persistence;
using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Api.Features.Setup;

public static class DemoSeeder
{
    public static async Task SeedAsync(IDbContextFactory<ApplicationDbContext> factory, TimeProvider clock, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        if (await database.Seasons.AnyAsync(cancellationToken)) throw new InvalidOperationException("Demo data requires an empty application database.");
        var season = new Season { Id = Guid.NewGuid(), Year = clock.GetUtcNow().Year };
        var club = new Club { Id = Guid.NewGuid(), Name = "Demoklub Nord" };
        var venue = new Venue { Id = Guid.NewGuid(), Name = "Demoanlage", Address = "Fiktive Beispieladresse" };
        venue.Lanes = [new Lane { VenueId = venue.Id, Number = 1 }, new Lane { VenueId = venue.Id, Number = 2 }];
        var annual = new AnnualChampionship { Id = Guid.NewGuid(), SeasonId = season.Id, Name = "Demo-Jahresmeisterschaft" };
        database.AddRange(season, club, venue, annual);
        for (var index = 1; index <= 4; index++)
        {
            var player = new Player { Id = Guid.NewGuid(), FirstName = "Beispiel", LastName = $"Spieler {index}", SfkvNumber = index == 4 ? null : $"9999999990{index}" };
            database.Players.Add(player);
            database.SeasonMembers.Add(new SeasonMember { SeasonId = season.Id, PlayerId = player.Id, ClubId = index == 4 ? null : club.Id, Category = index == 4 ? PlayerCategory.Guest : PlayerCategory.A });
        }
        database.Events.Add(new ChampionshipEvent
        {
            Id = Guid.NewGuid(), AnnualChampionshipId = annual.Id, Name = "Demo – Frühlingsmeisterschaft", VenueId = venue.Id,
            StartDate = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime), EndDate = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime), MaximumThrows = 100
        });
        database.Audit("DevelopmentDemoSeeded", season.Id.ToString(), "development-command", "explicit-demo-seed", clock.GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);
    }
}
