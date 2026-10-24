using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Championships;
using SwissBowling.Server.Features.Associations;
using SwissBowling.Server.Features.Results;

namespace SwissBowling.Server.Features.Championships;

/// <summary>Meisterschaft = Anlass auf einer Kegelbahn; jeder Kegler erzielt genau ein Resultat.</summary>
public class Championship
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [MaxLength(200)]
    public required string Name { get; set; }

    public Guid AssociationId { get; set; }

    public Association Association { get; set; } = null!;

    /// <summary>Durchführender Klub.</summary>
    [MaxLength(200)]
    public string? Organizer { get; set; }

    /// <summary>Lokal / Kegelbahn.</summary>
    [MaxLength(200)]
    public string? Venue { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    /// <summary>Anzahl Würfe pro Kegler (typisch 100 oder 120).</summary>
    public int Throws { get; set; }

    /// <summary>msID auf UVweb (nur bei importierten Meisterschaften).</summary>
    public int? ExternalId { get; set; }

    public List<Result> Results { get; set; } = [];

    public int MaxPins => Throws * 9;

    public void Apply(SaveChampionshipRequest request)
    {
        Name = request.Name.Trim();
        Organizer = Clean(request.Organizer);
        Venue = Clean(request.Venue);
        City = Clean(request.City);
        StartDate = request.StartDate;
        EndDate = request.EndDate;
        Throws = request.Throws;
    }

    public ChampionshipDto ToDto() =>
        new(Id, Name, AssociationId, Association.Name, Organizer, Venue, City, StartDate, EndDate, Throws, Results.Count, ExternalId);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
