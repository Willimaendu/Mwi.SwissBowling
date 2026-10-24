namespace SwissBowling.Core.Championships;

/// <summary>
/// SFKV-Rangierung: Resultat absteigend, dann Anzahl Neuner absteigend. Bei vollständigem Gleichstand wird der Rang geteilt.
/// </summary>
public static class RankingCalculator
{
    public static IReadOnlyList<(T Item, int Rank)> Rank<T>(IEnumerable<T> items, Func<T, int> pins, Func<T, int> nines)
    {
        var ordered = items.OrderByDescending(pins).ThenByDescending(nines).ToList();
        var ranked = new List<(T, int)>(ordered.Count);

        for (var i = 0; i < ordered.Count; i++)
        {
            var tied = i > 0 && pins(ordered[i - 1]) == pins(ordered[i]) && nines(ordered[i - 1]) == nines(ordered[i]);
            ranked.Add((ordered[i], tied ? ranked[i - 1].Item2 : i + 1));
        }

        return ranked;
    }
}
