using SwissBowling.Core.Players;
using SwissBowling.Core.Results;
using SwissBowling.Server.Features.Import;
using Xunit;

namespace SwissBowling.Tests;

public sealed class UvwebPdfParserTests
{
    [Fact]
    public void Liest_nur_Meisterschaft_und_MS_Damen_mit_Kategorien_und_Auszeichnungsgrenze()
    {
        string[] lines =
        [
            "Unterverband", "Emmental", "Rangliste", "46. Guet-Holz Meisterschaft und Americaine",
            "MS",
            "50010 / 201411 05.10.2020 - 18:30:05 - UVAEMRGA - 1/2 -",
            "Kategorie A (48 Teilnehmer)",
            "1. Wenger Fritz, Harder 826 / 49",
            "23. Muster Roberto, Zolli/Gäbelbach 771 / 30",
            "ohne Auszeichnung",
            "60. Kirsch Kurt, Rot - Weiss - Krone 670 / 25",
            "Nr. 3837",
            "MSD",
            "Kategorie B (3 Teilnehmer)",
            "1. Auderset Lydia, Caesar 799 / 30",
            "AME",
            "Kategorie A (11 Teilnehmer)",
            "1. Soltermann Jürg / Scheidegger Roland, River Boys / River Boys 644 / 31"
        ];

        var results = UvwebPdfParser.ParseLines(lines);

        Assert.Equal(4, results.Count);
        Assert.Equal(new UvwebArchiveResult("Wenger Fritz", "Harder", PlayerCategory.A, Discipline.Championship, 826, 49, Award: true), results[0]);
        Assert.Equal("Zolli/Gäbelbach", results[1].ClubName);
        Assert.False(results[2].Award);
        Assert.Equal(Discipline.Women, results[3].Discipline);
        Assert.Equal(PlayerCategory.B, results[3].Category);
    }

    [Fact]
    public void Ohne_Disziplin_Abschnitt_werden_keine_Resultate_uebernommen() =>
        Assert.Empty(UvwebPdfParser.ParseLines(["Kategorie A (1 Teilnehmer)", "1. Muster Hans, Blitz 700 / 20"]));
}
