namespace SwissBowling.Core.Venues;

/// <summary>Kegelbahn eines Unterverbands.</summary>
public sealed record VenueDto(Guid Id, string Name, string? Zip, string? City, string? Phone, int? Lanes, string? RestDay, string? Website, Guid AssociationId, string AssociationName);
