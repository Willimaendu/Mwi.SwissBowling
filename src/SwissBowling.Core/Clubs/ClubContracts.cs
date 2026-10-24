using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Players;

namespace SwissBowling.Core.Clubs;

public static class ClubRoutes
{
    public const string Base = "api/clubs";

    public static string ById(Guid id) => $"{Base}/{id}";
}

public sealed record ClubDto(
    Guid Id,
    string Name,
    string? City,
    PlayerCategory? Category,
    string? HomeVenue,
    string? Email,
    string? Website,
    Guid AssociationId,
    string AssociationName,
    int PlayerCount,
    int? ExternalId);

public sealed record SaveClubRequest(
    [property: Required, MaxLength(200)] string Name,
    [property: MaxLength(100)] string? City,
    PlayerCategory? Category,
    [property: MaxLength(200)] string? HomeVenue,
    [property: MaxLength(200), EmailAddress] string? Email,
    [property: MaxLength(200)] string? Website,
    [property: Required] Guid AssociationId);
