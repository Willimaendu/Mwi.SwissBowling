using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Clubs;
using SwissBowling.Server.Features.Associations;
using SwissBowling.Server.Features.Players;

namespace SwissBowling.Server.Features.Clubs;

public class Club
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    public Guid AssociationId { get; set; }

    public Association Association { get; set; } = null!;

    /// <summary>klubID auf UVweb (nur bei importierten Klubs).</summary>
    public int? ExternalId { get; set; }

    public List<Player> Players { get; set; } = [];

    public ClubDto ToDto() => new(Id, Name, City, AssociationId, Association.Name, Players.Count(p => p.IsActive), ExternalId);
}
