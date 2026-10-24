using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Championships;
using SwissBowling.Core.Players;

namespace SwissBowling.Core.Results;

public static class ResultRoutes
{
    /// <summary>Alle Resultate einer Runde lesen bzw. als Batch speichern (Upsert pro Kegler).</summary>
    public static string ForRound(Guid roundId) => $"{ChampionshipRoutes.RoundBase}/{roundId}/results";

    public static string ById(Guid resultId) => $"api/results/{resultId}";
}

public sealed record ResultDto(Guid Id, Guid PlayerId, string PlayerName, string ClubShortName, PlayerCategory Category, int Pins);

public sealed record RoundDetailDto(RoundDto Round, ChampionshipDto Championship, IReadOnlyList<ResultDto> Results);

public sealed record ResultInput([property: Required] Guid PlayerId, [property: Range(0, 9000)] int Pins);

public sealed record SaveResultsRequest([property: Required] IReadOnlyList<ResultInput> Results);
