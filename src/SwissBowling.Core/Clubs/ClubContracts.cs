using System.ComponentModel.DataAnnotations;

namespace SwissBowling.Core.Clubs;

public static class ClubRoutes
{
    public const string Base = "api/clubs";

    public static string ById(Guid id) => $"{Base}/{id}";
}

public sealed record ClubDto(Guid Id, string Name, string? City, Guid AssociationId, string AssociationName, int PlayerCount, int? ExternalId);

public sealed record SaveClubRequest(
    [property: Required, MaxLength(200)] string Name,
    [property: MaxLength(100)] string? City,
    [property: Required] Guid AssociationId);
