using System.ComponentModel.DataAnnotations;

namespace SwissBowling.Core.Seasons;

public static class SeasonRoutes
{
    public const string Base = "api/seasons";

    public static string ById(Guid id) => $"{Base}/{id}";
}

public sealed record SeasonDto(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate, int ChampionshipCount);

public sealed record SaveSeasonRequest(
    [property: Required, MaxLength(50)] string Name,
    DateOnly StartDate,
    DateOnly EndDate);
