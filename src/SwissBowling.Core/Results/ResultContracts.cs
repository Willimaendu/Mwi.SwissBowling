using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Championships;
using SwissBowling.Core.Players;

namespace SwissBowling.Core.Results;

public static class ResultRoutes
{
    /// <summary>Alle Resultate einer Meisterschaft lesen bzw. als Batch speichern (Upsert pro Kegler).</summary>
    public static string ForChampionship(Guid championshipId) => $"{ChampionshipRoutes.Base}/{championshipId}/results";

    public static string ById(Guid resultId) => $"api/results/{resultId}";
}

public sealed record ResultDto(Guid Id, Guid PlayerId, string PlayerName, string ClubName, PlayerCategory Category, int Pins, int Nines, bool Award, DateOnly? Date);

public sealed record ResultInput([property: Required] Guid PlayerId, [property: Range(0, 9000)] int Pins, [property: Range(0, 1000)] int Nines, bool Award, DateOnly? Date);

public sealed record SaveResultsRequest([property: Required] IReadOnlyList<ResultInput> Results);
