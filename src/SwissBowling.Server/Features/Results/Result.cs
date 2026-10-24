using SwissBowling.Core.Results;
using SwissBowling.Server.Features.Championships;
using SwissBowling.Server.Features.Players;

namespace SwissBowling.Server.Features.Results;

/// <summary>Resultat eines Keglers an einer Meisterschaft.</summary>
public class Result
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ChampionshipId { get; set; }

    public Championship Championship { get; set; } = null!;

    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    /// <summary>Total Holz.</summary>
    public int Pins { get; set; }

    /// <summary>Anzahl Neunerwürfe (zweites Rangierungskriterium).</summary>
    public int Nines { get; set; }

    /// <summary>Auszeichnung erreicht.</summary>
    public bool Award { get; set; }

    /// <summary>Matchdatum.</summary>
    public DateOnly? Date { get; set; }

    public ResultDto ToDto() => new(Id, PlayerId, Player.Name, Player.Club.Name, Player.Category, Pins, Nines, Award, Date);
}
