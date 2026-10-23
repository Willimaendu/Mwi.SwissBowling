using Microsoft.EntityFrameworkCore;
using Mwi.SwissBowling.Api.Persistence;
using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Api.Features.Results;

public static class ResultQueries
{
    public static Task<ResultDto[]> ReadAsync(ApplicationDbContext database, Guid eventId, CancellationToken cancellationToken) =>
        (from result in database.Set<PlayerResult>().AsNoTracking()
         join member in database.SeasonMembers on new { result.SeasonId, result.PlayerId } equals new { member.SeasonId, member.PlayerId }
         join player in database.Players on result.PlayerId equals player.Id
         join club in database.Clubs on member.ClubId equals club.Id into clubs
         from club in clubs.DefaultIfEmpty()
         where result.EventId == eventId
         orderby player.LastName, player.FirstName, player.Id
         select new ResultDto(player.Id, player.FirstName + " " + player.LastName, club == null ? null : club.Name, member.Category,
             result.Total, result.Nines, result.Eights, result.Interrupted, result.Version)).ToArrayAsync(cancellationToken);
}
