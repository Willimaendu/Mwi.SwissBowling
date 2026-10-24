using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Championships;
using SwissBowling.Server.Features.Results;
using SwissBowling.Server.Features.Seasons;

namespace SwissBowling.Server.Features.Championships;

public class Championship
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public Guid SeasonId { get; set; }

    public Season Season { get; set; } = null!;

    public int NumberOfRounds { get; set; }

    /// <summary>Anzahl Streichresultate (schlechteste Runden ohne Wertung).</summary>
    public int DiscardedRounds { get; set; }

    /// <summary>Würfe pro Runde und Kegler (typisch 120).</summary>
    public int ThrowsPerRound { get; set; }

    public List<Round> Rounds { get; set; } = [];

    public int CountedRounds => NumberOfRounds - DiscardedRounds;

    public int MaxPinsPerRound => ThrowsPerRound * 9;

    public void Apply(SaveChampionshipRequest request)
    {
        Name = request.Name.Trim();
        Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        NumberOfRounds = request.NumberOfRounds;
        DiscardedRounds = request.DiscardedRounds;
        ThrowsPerRound = request.ThrowsPerRound;
    }

    public ChampionshipDto ToDto(int participantCount) =>
        new(Id, Name, Description, SeasonId, Season.Name, NumberOfRounds, DiscardedRounds, ThrowsPerRound,
            Rounds.Count, Rounds.Count(r => r.IsCompleted), participantCount);
}

public class Round
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ChampionshipId { get; set; }

    public Championship Championship { get; set; } = null!;

    /// <summary>Laufende Nummer innerhalb der Meisterschaft (1-basiert).</summary>
    public int Number { get; set; }

    public DateOnly Date { get; set; }

    [MaxLength(200)]
    public required string Venue { get; set; }

    /// <summary>Abgeschlossene Runden dürfen nur noch von Administratoren geändert werden.</summary>
    public bool IsCompleted { get; set; }

    public List<Result> Results { get; set; } = [];

    public RoundDto ToDto() => new(Id, ChampionshipId, Championship.Name, Number, Date, Venue, IsCompleted, Results.Count);
}
