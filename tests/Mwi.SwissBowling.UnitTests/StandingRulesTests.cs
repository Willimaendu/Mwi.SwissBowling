using Mwi.SwissBowling.Api.Features.Standings;
using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.UnitTests;

public sealed class StandingRulesTests
{
    private static ResultDto Result(string name, int total, int nines, int eights, PlayerCategory category = PlayerCategory.A, bool interrupted = false) => new(Guid.NewGuid(), name, "Beispielklub", category, total, nines, eights, interrupted, 1);
    [Fact]
    public void ExactTiesShareRankWithGaps()
    {
        var rows = StandingRules.Calculate([Result("B", 764, 27, 22), Result("A", 764, 27, 22), Result("C", 751, 25, 21)]);
        Assert.Equal([1, 1, 3], rows.Select(row => row.Rank).ToArray());
        Assert.Equal(["A", "B", "C"], rows.Select(row => row.Name).ToArray());
    }
    [Fact]
    public void TotalThenNinesThenEightsDetermineOrder()
    {
        var rows = StandingRules.Calculate([Result("D", 750, 30, 30), Result("C", 760, 20, 19), Result("B", 760, 20, 20), Result("A", 760, 21, 0)]);
        Assert.Equal(["A", "B", "C", "D"], rows.Select(row => row.Name).ToArray());
    }
    [Fact]
    public void EachCategoryIncludingGuestsStartsAtOne()
    {
        var rows = StandingRules.Calculate([Result("Gast", 900, 100, 0, PlayerCategory.Guest), Result("B", 760, 20, 20, PlayerCategory.B), Result("A", 750, 20, 20)]);
        Assert.All(rows, row => Assert.Equal(1, row.Rank));
        Assert.Equal([PlayerCategory.A, PlayerCategory.B, PlayerCategory.Guest], rows.Select(row => row.Category).ToArray());
    }
    [Fact]
    public void InterruptedResultRemainsInStandings()
    {
        var row = Assert.Single(StandingRules.Calculate([Result("Teilresultat", 80, 0, 10, interrupted: true)]));
        Assert.Equal(80, row.Total);
        Assert.True(row.Interrupted);
    }
    [Fact]
    public void EmptyInputHasNoStandings() => Assert.Empty(StandingRules.Calculate([]));
}
