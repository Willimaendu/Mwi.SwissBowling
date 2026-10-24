using System.ComponentModel.DataAnnotations;

namespace SwissBowling.Core.Clubs;

public static class ClubRoutes
{
    public const string Base = "api/clubs";

    public static string ById(Guid id) => $"{Base}/{id}";
}

public sealed record ClubDto(Guid Id, string Name, string ShortName, string? City, int PlayerCount);

public sealed record SaveClubRequest(
    [property: Required, MaxLength(200)] string Name,
    [property: Required, MaxLength(10)] string ShortName,
    [property: MaxLength(100)] string? City);
