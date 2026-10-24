namespace SwissBowling.Core.Championships;

public readonly record struct PlayerRoundResult(Guid PlayerId, int RoundNumber, int Pins);

public sealed record RankingEntry(
    Guid PlayerId,
    int Rank,
    int Total,
    double Average,
    int Best,
    int RoundsPlayed,
    IReadOnlyDictionary<int, int> PinsByRound,
    IReadOnlyCollection<int> DiscardedRoundNumbers);

/// <summary>
/// Rangliste einer Einzelmeisterschaft: Pro Kegler zählen die besten <c>countedRounds</c> Resultate.
/// Sortierung nach Total, dann bestem Einzelresultat; bei vollständigem Gleichstand wird der Rang geteilt.
/// </summary>
public static class RankingCalculator
{
    public static IReadOnlyList<RankingEntry> Calculate(IEnumerable<PlayerRoundResult> results, int countedRounds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(countedRounds, 1);

        var ordered = results
            .GroupBy(r => r.PlayerId)
            .Select(group =>
            {
                var byRound = group.GroupBy(r => r.RoundNumber).ToDictionary(g => g.Key, g => g.Max(r => r.Pins));
                var sorted = byRound.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).ToList();
                var counted = sorted.Take(countedRounds).Select(kv => kv.Value).ToList();

                return new RankingEntry(
                    group.Key,
                    Rank: 0,
                    Total: counted.Sum(),
                    Average: counted.Count == 0 ? 0 : Math.Round(counted.Average(), 2),
                    Best: counted.Count == 0 ? 0 : counted.Max(),
                    RoundsPlayed: byRound.Count,
                    PinsByRound: byRound,
                    DiscardedRoundNumbers: sorted.Skip(countedRounds).Select(kv => kv.Key).ToList());
            })
            .OrderByDescending(e => e.Total)
            .ThenByDescending(e => e.Best)
            .ToList();

        var ranked = new List<RankingEntry>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var tied = i > 0 && ordered[i - 1].Total == ordered[i].Total && ordered[i - 1].Best == ordered[i].Best;
            ranked.Add(ordered[i] with { Rank = tied ? ranked[i - 1].Rank : i + 1 });
        }

        return ranked;
    }
}
