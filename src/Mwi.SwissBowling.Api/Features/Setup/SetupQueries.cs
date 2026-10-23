using Microsoft.EntityFrameworkCore;
using Mwi.SwissBowling.Api.Persistence;
using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Api.Features.Setup;

public static class SetupQueries
{
    public static async Task<SetupDto> ReadAsync(IDbContextFactory<ApplicationDbContext> factory, CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        return new(
        await database.Seasons.AsNoTracking().OrderByDescending(row => row.Year).Select(row => new SeasonDto(row.Id, row.Year)).ToArrayAsync(cancellationToken),
        await database.Clubs.AsNoTracking().OrderBy(row => row.Name).Select(row => new ClubDto(row.Id, row.Name, row.Email, row.Phone)).ToArrayAsync(cancellationToken),
        await database.Players.AsNoTracking().OrderBy(row => row.LastName).ThenBy(row => row.FirstName).Select(row => new PlayerDto(row.Id, row.FirstName, row.LastName, row.SfkvNumber, row.DateOfBirth, row.Email, row.Phone)).ToArrayAsync(cancellationToken),
        await database.SeasonMembers.AsNoTracking().Select(row => new SeasonMemberDto(row.SeasonId, row.PlayerId, row.ClubId, row.Category)).ToArrayAsync(cancellationToken),
        await database.Venues.AsNoTracking().OrderBy(row => row.Name).Select(row => new VenueDto(row.Id, row.Name, row.Address, row.Lanes.OrderBy(lane => lane.Number).Select(lane => lane.Number).ToArray())).ToArrayAsync(cancellationToken),
        await database.AnnualChampionships.AsNoTracking().Select(row => new AnnualChampionshipDto(row.Id, row.SeasonId, row.Name)).ToArrayAsync(cancellationToken),
        await database.Events.AsNoTracking().OrderByDescending(row => row.StartDate).Select(row => new EventDto(row.Id, row.AnnualChampionshipId, row.Name, row.VenueId, row.StartDate, row.EndDate, row.MaximumThrows, row.State, row.PublishedRevision)).ToArrayAsync(cancellationToken));
    }
}
