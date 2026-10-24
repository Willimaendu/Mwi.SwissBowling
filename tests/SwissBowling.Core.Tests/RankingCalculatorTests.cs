using SwissBowling.Core.Championships;
using Xunit;

namespace SwissBowling.Core.Tests;

public sealed class RankingCalculatorTests
{
    private static readonly Guid Anna = Guid.NewGuid();
    private static readonly Guid Beat = Guid.NewGuid();
    private static readonly Guid Carla = Guid.NewGuid();

    [Fact]
    public void Streicht_das_schlechteste_Resultat()
    {
        var entry = Assert.Single(RankingCalculator.Calculate(
            [new(Anna, 1, 600), new(Anna, 2, 550), new(Anna, 3, 620)], countedRounds: 2));

        Assert.Equal(1220, entry.Total);
        Assert.Equal(620, entry.Best);
        Assert.Equal(610, entry.Average);
        Assert.Equal(3, entry.RoundsPlayed);
        Assert.Equal(new[] { 2 }, entry.DiscardedRoundNumbers);
    }

    [Fact]
    public void Sortiert_nach_Total_dann_bestem_Einzelresultat()
    {
        var ranking = RankingCalculator.Calculate(
            [new(Anna, 1, 600), new(Anna, 2, 600), new(Beat, 1, 650), new(Beat, 2, 550), new(Carla, 1, 500), new(Carla, 2, 510)],
            countedRounds: 2);

        Assert.Equal(new[] { Beat, Anna, Carla }, ranking.Select(r => r.PlayerId));
        Assert.Equal(new[] { 1, 2, 3 }, ranking.Select(r => r.Rank));
    }

    [Fact]
    public void Vollstaendiger_Gleichstand_teilt_den_Rang()
    {
        var ranking = RankingCalculator.Calculate([new(Anna, 1, 600), new(Beat, 1, 600), new(Carla, 1, 590)], countedRounds: 1);

        Assert.Equal(new[] { 1, 1, 3 }, ranking.Select(r => r.Rank));
    }

    [Fact]
    public void Leere_Eingabe_liefert_leere_Rangliste() =>
        Assert.Empty(RankingCalculator.Calculate([], countedRounds: 1));
}
