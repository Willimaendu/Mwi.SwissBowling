using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Results;

namespace SwissBowling.Core.Players;

public static class PlayerRoutes
{
    public const string Base = "api/players";

    public static string ById(Guid id) => $"{Base}/{id}";

    public static string All(bool includeInactive) => $"{Base}?includeInactive={includeInactive}";
}

/// <summary>SFKV-Kategorien: A (stärkste), B, C und G (Gäste/Jüngere).</summary>
public enum PlayerCategory
{
    A = 1,
    B = 2,
    C = 3,
    G = 4
}

/// <summary>Name im SFKV-Format «Nachname Vorname». Kegler ohne Klub sind Einzelmitglieder ihres Unterverbands.</summary>
public sealed record PlayerDto(
    Guid Id,
    string Name,
    string? City,
    string? LicenseNumber,
    DateOnly? BirthDate,
    PlayerCategory Category,
    bool IsActive,
    Guid? ClubId,
    string? ClubName,
    Guid AssociationId,
    string AssociationName);

public sealed record SavePlayerRequest(
    [property: Required, MaxLength(200)] string Name,
    [property: MaxLength(100)] string? City,
    [property: MaxLength(20)] string? LicenseNumber,
    DateOnly? BirthDate,
    PlayerCategory Category,
    Guid? ClubId,
    [property: Required] Guid AssociationId,
    bool IsActive = true);

public sealed record PlayerResultDto(
    Guid ChampionshipId,
    string ChampionshipName,
    Discipline Discipline,
    DateOnly? Date,
    int Pins,
    int Nines,
    bool Award,
    int Rank,
    int Participants);

public sealed record PlayerDetailDto(PlayerDto Player, IReadOnlyList<PlayerResultDto> Results);
