using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Players;
using SwissBowling.Core.Results;

namespace SwissBowling.Core.Championships;

public static class ChampionshipRoutes
{
    public const string Base = "api/championships";

    public static string ById(Guid id) => $"{Base}/{id}";

    public static string ByYear(int? year) => year is null ? Base : $"{Base}?year={year}";
}

public enum ChampionshipStatus
{
    Planned,
    Running,
    Completed
}

/// <summary>Eine Meisterschaft ist ein Anlass (typisch 1–2 Wochen auf einer Kegelbahn), bei dem jeder Kegler ein Resultat pro Disziplin erzielt.</summary>
public sealed record ChampionshipDto(
    Guid Id,
    string Name,
    Guid AssociationId,
    string AssociationName,
    string? Organizer,
    string? Venue,
    string? City,
    DateOnly StartDate,
    DateOnly EndDate,
    int Throws,
    int ResultCount,
    int? ExternalId)
{
    /// <summary>Maximal mögliches Resultat (9 Holz pro Wurf).</summary>
    public int MaxPins => Throws * 9;

    public ChampionshipStatus StatusAt(DateOnly today) =>
        today < StartDate ? ChampionshipStatus.Planned
        : today > EndDate ? ChampionshipStatus.Completed
        : ChampionshipStatus.Running;
}

public sealed record SaveChampionshipRequest(
    [property: Required, MaxLength(200)] string Name,
    [property: Required] Guid AssociationId,
    [property: MaxLength(200)] string? Organizer,
    [property: MaxLength(200)] string? Venue,
    [property: MaxLength(100)] string? City,
    DateOnly StartDate,
    DateOnly EndDate,
    [property: Range(1, 1000)] int Throws);

public sealed record RankingEntryDto(int Rank, Guid PlayerId, string PlayerName, string? ClubName, Discipline Discipline, PlayerCategory Category, int Pins, int Nines, bool Award);

public sealed record ChampionshipDetailDto(ChampionshipDto Championship, IReadOnlyList<RankingEntryDto> Ranking);
