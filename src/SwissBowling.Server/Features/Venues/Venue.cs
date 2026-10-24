using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Venues;
using SwissBowling.Server.Features.Associations;

namespace SwissBowling.Server.Features.Venues;

/// <summary>Kegelbahn (Lokal mit Bahnen) eines Unterverbands.</summary>
public class Venue
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid AssociationId { get; set; }

    public Association Association { get; set; } = null!;

    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(10)]
    public string? Zip { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    public int? Lanes { get; set; }

    [MaxLength(100)]
    public string? RestDay { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }

    public VenueDto ToDto() => new(Id, Name, Zip, City, Phone, Lanes, RestDay, Website, AssociationId, Association.Name);
}
