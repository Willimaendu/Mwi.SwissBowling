using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Players;

namespace SwissBowling.Core.Championships;

public static class ChampionshipRoutes
{
    public const string Base = "api/championships";

    public static string ById(Guid id) => $"{Base}/{id}";

    public static string BySeason(Guid? seasonId) => seasonId is null ? Base : $"{Base}?seasonId={seasonId}";

    public static string Rounds(Guid id) => $"{Base}/{id}/rounds";

    public static string Ranking(Guid id, PlayerCategory? category) =>
        category is null ? $"{Base}/{id}/ranking" : $"{Base}/{id}/ranking?category={(int)category}";

    public const string RoundBase = "api/rounds";

    public static string Round(Guid roundId) => $"{RoundBase}/{roundId}";

    public static string RoundCompletion(Guid roundId) => $"{RoundBase}/{roundId}/completed";
}

public enum ChampionshipStatus
{
    Planned,
    Running,
    Completed
}

public sealed record ChampionshipDto(
    Guid Id,
    string Name,
    string? Description,
    Guid SeasonId,
    string SeasonName,
    int NumberOfRounds,
    int DiscardedRounds,
    int ThrowsPerRound,
    int RoundsCreated,
    int RoundsCompleted,
    int ParticipantCount)
{
    /// <summary>Anzahl Runden, die in die Wertung zählen.</summary>
    public int CountedRounds => NumberOfRounds - DiscardedRounds;

    /// <summary>Maximal mögliches Resultat pro Runde (9 Holz pro Wurf).</summary>
    public int MaxPinsPerRound => ThrowsPerRound * 9;

    /// <summary>Status wird aus den abgeschlossenen Runden abgeleitet – kein separat zu pflegender Zustand.</summary>
    public ChampionshipStatus Status =>
        RoundsCompleted == 0 ? ChampionshipStatus.Planned
        : RoundsCompleted >= NumberOfRounds ? ChampionshipStatus.Completed
        : ChampionshipStatus.Running;
}

public sealed record SaveChampionshipRequest(
    [property: Required, MaxLength(200)] string Name,
    [property: MaxLength(2000)] string? Description,
    [property: Required] Guid SeasonId,
    [property: Range(1, 50)] int NumberOfRounds,
    [property: Range(0, 49)] int DiscardedRounds,
    [property: Range(1, 1000)] int ThrowsPerRound);

public sealed record RoundDto(
    Guid Id,
    Guid ChampionshipId,
    string ChampionshipName,
    int Number,
    DateOnly Date,
    string Venue,
    bool IsCompleted,
    int ResultCount);

public sealed record SaveRoundRequest(DateOnly Date, [property: Required, MaxLength(200)] string Venue);

public sealed record SetRoundCompletedRequest(bool IsCompleted);

public sealed record ChampionshipDetailDto(ChampionshipDto Championship, IReadOnlyList<RoundDto> Rounds);

public sealed record RankingEntryDto(
    int Rank,
    Guid PlayerId,
    string PlayerName,
    string ClubShortName,
    PlayerCategory Category,
    int Total,
    double Average,
    int Best,
    int RoundsPlayed,
    IReadOnlyDictionary<int, int> PinsByRound,
    IReadOnlyCollection<int> DiscardedRoundNumbers);

public sealed record RankingDto(
    ChampionshipDto Championship,
    PlayerCategory? Category,
    IReadOnlyList<int> RoundNumbers,
    IReadOnlyList<RankingEntryDto> Entries);
