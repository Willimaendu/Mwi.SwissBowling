using System.ComponentModel.DataAnnotations;
using SwissBowling.Server.Features.Players;

namespace SwissBowling.Server.Features.Clubs;

public class Club
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(10)]
    public required string ShortName { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    public List<Player> Players { get; set; } = [];
}
