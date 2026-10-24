using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Associations;
using SwissBowling.Server.Features.Clubs;

namespace SwissBowling.Server.Features.Associations;

/// <summary>Unterverband der SFKV.</summary>
public class Association
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>uvID auf UVweb (nur bei importierten Verbänden).</summary>
    public int? ExternalId { get; set; }

    public List<Club> Clubs { get; set; } = [];

    public AssociationDto ToDto() => new(Id, Name, ExternalId, Clubs.Count);
}
