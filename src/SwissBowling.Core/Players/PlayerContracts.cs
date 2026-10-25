using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Results;

namespace SwissBowling.Core.Players;

public static class PlayerRoutes
{
    public const string Base = "api/players";

    public static string ById(Guid id) => $"{Base}/{id}";

    /// <summary>Obergrenze pro Seite (für Listen eines einzelnen Unterverbands, z.B. Resultaterfassung).</summary>
    public const int MaxPageSize = 1000;

    /// <summary>Serverseitig gefilterte, seitenweise Liste – die Keglerliste wird nie komplett in den Browser geladen.</summary>
    public static string Search(PlayerFilter filter, int page, int pageSize)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query.Add($"search={Uri.EscapeDataString(filter.Search.Trim())}");
        }

        if (filter.ClubId is { } club)
        {
            query.Add($"clubId={club}");
        }

        if (filter.AssociationId is { } association)
        {
            query.Add($"associationId={association}");
        }

        if (filter.Category is { } category)
        {
            query.Add($"category={(int)category}");
        }

        if (filter.IncludeInactive)
        {
            query.Add("includeInactive=true");
        }

        return $"{Base}?{string.Join('&', query)}";
    }
}

/// <summary>Filter der Keglerliste; Suche über Name, Ort und Lizenznummer.</summary>
public sealed record PlayerFilter(string? Search = null, Guid? ClubId = null, Guid? AssociationId = null, PlayerCategory? Category = null, bool IncludeInactive = false);

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
    DateOnly Date,
    int Throws,
    int Pins,
    int Nines,
    bool Award,
    int Rank,
    int Participants)
{
    /// <summary>Holz pro Wurf – die vergleichbare Grösse über Meisterschaften mit unterschiedlicher Wurfzahl.</summary>
    public double Average => Throws == 0 ? 0 : (double)Pins / Throws;
}

public sealed record PlayerDetailDto(PlayerDto Player, IReadOnlyList<PlayerResultDto> Results);
