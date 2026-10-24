using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Clubs;
using SwissBowling.Core.Players;
using SwissBowling.Server.Features.Associations;
using SwissBowling.Server.Features.Players;

namespace SwissBowling.Server.Features.Clubs;

/// <summary>Kegelklub. Name entspricht dem SFKV-Kurznamen (z.B. «Lüderenblick»).</summary>
public class Club
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [MaxLength(200)]
    public required string Name { get; set; }

    /// <summary>Ort des Klubs (sfkv.ch führt ihn in der Spalte «Heimbahn» hinter dem Komma).</summary>
    [MaxLength(100)]
    public string? City { get; set; }

    /// <summary>Klubkategorie A/B/C (Stärkeklasse des Klubs).</summary>
    public PlayerCategory? Category { get; set; }

    /// <summary>Heimbahn (Lokal), z.B. «Kegelsporthalle Allmend».</summary>
    [MaxLength(200)]
    public string? HomeVenue { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }

    public Guid AssociationId { get; set; }

    public Association Association { get; set; } = null!;

    /// <summary>Klub-ID der SFKV – identisch mit der klubID auf UVweb.</summary>
    public int? ExternalId { get; set; }

    public List<Player> Players { get; set; } = [];

    public ClubDto ToDto(int? activePlayers = null) =>
        new(Id, Name, City, Category, HomeVenue, Email, Website, AssociationId, Association.Name, activePlayers ?? Players.Count(p => p.IsActive), ExternalId);

    public void Apply(SaveClubRequest request)
    {
        Name = request.Name.Trim();
        City = Clean(request.City);
        Category = request.Category;
        HomeVenue = Clean(request.HomeVenue);
        Email = Clean(request.Email);
        Website = Clean(request.Website);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
