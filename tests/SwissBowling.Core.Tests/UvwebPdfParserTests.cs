using SwissBowling.Core.Players;
using SwissBowling.Server.Features.Import;
using Xunit;

namespace SwissBowling.Core.Tests;

public sealed class UvwebPdfParserTests
{
    [Fact]
    public void Liest_Kategorien_Resultate_und_Auszeichnungsgrenze()
    {
        string[] lines =
        [
            "Unterverband", "Emmental", "Rangliste", "25. Lüderenblick - Meisterschaft",
            "Kategorie A (66 Teilnehmer)",
            "1. Wicki Fridolin, River Boys 795 / 38",
            "20. Wyss Maria-Theresia, Haub-Haub 2 739 / 36",
            "ohne Auszeichnung",
            "28. Huber Otto, Hartholz 728 / 25",
            "60. Kirsch Kurt, Rot - Weiss - Krone 670 / 25",
            "Nr. 5068",
            "Kategorie G (5 Teilnehmer)",
            "1. Egli Helen, Langnau im Emmental 344 / 0"
        ];

        var results = UvwebPdfParser.ParseLines(lines);

        Assert.Equal(5, results.Count);
        Assert.Equal(new UvwebArchiveResult("Wicki Fridolin", "River Boys", PlayerCategory.A, 795, 38, Award: true), results[0]);
        Assert.Equal("Haub-Haub 2", results[1].ClubName);
        Assert.False(results[2].Award);
        Assert.Equal("Rot - Weiss - Krone", results[3].ClubName);
        Assert.Equal(PlayerCategory.G, results[4].Category);
        Assert.True(results[4].Award);
    }
}
