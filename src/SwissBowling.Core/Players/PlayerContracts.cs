using System.ComponentModel.DataAnnotations;

namespace SwissBowling.Core.Players;

public static class PlayerRoutes
{
    public const string Base = "api/players";

    public static string ById(Guid id) => $"{Base}/{id}";

    public static string All(bool includeInactive) => $"{Base}?includeInactive={includeInactive}";
}

public enum PlayerCategory
{
    Herren = 1,
    Damen = 2,
    Senioren = 3,
    Seniorinnen = 4,
    Junioren = 5,
    Juniorinnen = 6
}

public sealed record PlayerDto(
    Guid Id,
    string FirstName,
    string LastName,
    int BirthYear,
    PlayerCategory Category,
    string? LicenseNumber,
    bool IsActive,
    Guid ClubId,
    string ClubName,
    string ClubShortName)
{
    public string FullName => $"{FirstName} {LastName}";
}

public sealed record SavePlayerRequest(
    [property: Required, MaxLength(100)] string FirstName,
    [property: Required, MaxLength(100)] string LastName,
    [property: Range(1900, 2100)] int BirthYear,
    PlayerCategory Category,
    [property: MaxLength(50)] string? LicenseNumber,
    [property: Required] Guid ClubId,
    bool IsActive = true);

/// <summary>Ein Resultat im Keglerprofil.</summary>
public sealed record PlayerResultDto(
    Guid RoundId,
    Guid ChampionshipId,
    string ChampionshipName,
    int RoundNumber,
    DateOnly Date,
    string Venue,
    int Pins);

public sealed record PlayerDetailDto(PlayerDto Player, IReadOnlyList<PlayerResultDto> Results);
