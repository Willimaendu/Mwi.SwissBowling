using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Championships;
using SwissBowling.Core.Players;

namespace SwissBowling.Core.Results;

public static class ResultRoutes
{
    /// <summary>Alle Resultate einer Meisterschaft lesen bzw. als Batch speichern (Upsert pro Kegler und Disziplin).</summary>
    public static string ForChampionship(Guid championshipId) => $"{ChampionshipRoutes.Base}/{championshipId}/results";

    public static string ById(Guid resultId) => $"api/results/{resultId}";
}

/// <summary>Berücksichtigte Disziplinen (UVweb DiszID 1 und 2). Sport, Americaine, Sport Spezial und Kombination werden nicht erfasst.</summary>
public enum Discipline
{
    /// <summary>Meisterschaft (Herren und Damen in den Kategorien A/B/C/G).</summary>
    Championship = 1,

    /// <summary>MS Damen – separate Damenwertung.</summary>
    Women = 2
}

public sealed record ResultDto(Guid Id, Guid PlayerId, string PlayerName, string? ClubName, PlayerCategory Category, Discipline Discipline, int Pins, int Nines, bool Award, DateOnly? Date);

public sealed record ResultInput([property: Required] Guid PlayerId, Discipline Discipline, [property: Range(0, 9000)] int Pins, [property: Range(0, 1000)] int Nines, bool Award, DateOnly? Date);

public sealed record SaveResultsRequest([property: Required] IReadOnlyList<ResultInput> Results);
