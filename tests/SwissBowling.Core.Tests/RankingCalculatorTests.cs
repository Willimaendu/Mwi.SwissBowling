using SwissBowling.Core.Championships;
using Xunit;

namespace SwissBowling.Core.Tests;

public sealed class RankingCalculatorTests
{
    private sealed record Score(string Name, int Pins, int Nines);

    private static IReadOnlyList<(Score Item, int Rank)> Rank(params Score[] scores) =>
        RankingCalculator.Rank(scores, s => s.Pins, s => s.Nines);

    [Fact]
    public void Sortiert_nach_Holz_dann_Neuner()
    {
        var ranking = Rank(new("Anna", 738, 35), new("Beat", 770, 38), new("Carla", 738, 38));

        Assert.Equal(new[] { "Beat", "Carla", "Anna" }, ranking.Select(r => r.Item.Name));
        Assert.Equal(new[] { 1, 2, 3 }, ranking.Select(r => r.Rank));
    }

    [Fact]
    public void Vollstaendiger_Gleichstand_teilt_den_Rang()
    {
        var ranking = Rank(new("Anna", 700, 20), new("Beat", 700, 20), new("Carla", 690, 25));

        Assert.Equal(new[] { 1, 1, 3 }, ranking.Select(r => r.Rank));
    }

    [Fact]
    public void Leere_Eingabe_liefert_leere_Rangliste() => Assert.Empty(Rank());
}
