using SwissBowling.Core.Results;
using SwissBowling.Server.Features.Championships;
using SwissBowling.Server.Features.Players;

namespace SwissBowling.Server.Features.Results;

/// <summary>Resultat (Total Holz) eines Keglers in einer Runde.</summary>
public class Result
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RoundId { get; set; }

    public Round Round { get; set; } = null!;

    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public int Pins { get; set; }

    public ResultDto ToDto() => new(Id, PlayerId, $"{Player.FirstName} {Player.LastName}", Player.Club.ShortName, Player.Category, Pins);
}
