using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Associations;
using SwissBowling.Server.Features.Clubs;
using SwissBowling.Server.Features.Players;
using SwissBowling.Server.Features.Venues;

namespace SwissBowling.Server.Features.Associations;

/// <summary>Unterverband der SFKV. Stammdaten-Master: www.sfkv.ch/unterverbaende.</summary>
public class Association
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>Kürzel, z.B. «uvem».</summary>
    [MaxLength(10)]
    public string? ShortCode { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }

    /// <summary>UV-Nr. – identisch mit der uvID auf UVweb.</summary>
    public int? ExternalId { get; set; }

    public List<Club> Clubs { get; set; } = [];

    public List<Player> Players { get; set; } = [];

    public List<Venue> Venues { get; set; } = [];

    public List<BoardMember> Board { get; set; } = [];

    public AssociationDto ToDto(int? clubCount = null, int? playerCount = null) =>
        new(Id, Name, ShortCode, Website, ExternalId, clubCount ?? Clubs.Count, playerCount ?? Players.Count(p => p.IsActive));
}

/// <summary>Vorstandsmitglied eines Unterverbands (öffentliche Funktionärsdaten gemäss sfkv.ch).</summary>
public class BoardMember
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid AssociationId { get; set; }

    public Association Association { get; set; } = null!;

    [MaxLength(100)]
    public required string Function { get; set; }

    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    public DateOnly? Since { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    public BoardMemberDto ToDto() => new(Function, Name, City, Since, Email, Phone);
}
