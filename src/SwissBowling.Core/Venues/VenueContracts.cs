namespace SwissBowling.Core.Venues;

public static class VenueRoutes
{
    public const string Base = "api/venues";
}

/// <summary>Kegelbahn eines Unterverbands.</summary>
public sealed record VenueDto(Guid Id, string Name, string? Zip, string? City, string? Phone, int? Lanes, string? RestDay, string? Website, Guid AssociationId, string AssociationName);
