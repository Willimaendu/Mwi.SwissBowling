using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Api.Features.Standings;

public static class StandingRules
{
    public static RankedRow[] Calculate(IEnumerable<ResultDto> results)
    {
        var standings = new List<RankedRow>();
        foreach (var category in results.GroupBy(result => result.Category).OrderBy(group => group.Key))
        {
            var position = 0;
            var rank = 0;
            (int Total, int Nines, int Eights)? previous = null;
            foreach (var result in category.OrderByDescending(row => row.Total).ThenByDescending(row => row.Nines).ThenByDescending(row => row.Eights).ThenBy(row => row.Name, StringComparer.Ordinal).ThenBy(row => row.PlayerId))
            {
                position++;
                var score = (result.Total, result.Nines, result.Eights);
                if (previous != score) rank = position;
                standings.Add(new RankedRow(rank, result.Name, result.Club, result.Category, result.Total, result.Nines, result.Eights, result.Interrupted));
                previous = score;
            }
        }
        return standings.ToArray();
    }
}
