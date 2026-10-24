using System.ComponentModel.DataAnnotations;
using SwissBowling.Core.Clubs;
using SwissBowling.Core.Venues;

namespace SwissBowling.Core.Associations;

public static class AssociationRoutes
{
    public const string Base = "api/associations";

    public static string ById(Guid id) => $"{Base}/{id}";
}

/// <summary>Unterverband (UV) der SFKV, z.B. Emmental (uvem, UV-Nr. 15).</summary>
public sealed record AssociationDto(Guid Id, string Name, string? ShortCode, string? Website, int? ExternalId, int ClubCount, int PlayerCount);

public sealed record BoardMemberDto(string Function, string Name, string? City, DateOnly? Since, string? Email, string? Phone);

public sealed record AssociationDetailDto(AssociationDto Association, IReadOnlyList<BoardMemberDto> Board, IReadOnlyList<ClubDto> Clubs, IReadOnlyList<VenueDto> Venues);

public sealed record SaveAssociationRequest([property: Required, MaxLength(100)] string Name, [property: MaxLength(10)] string? ShortCode, [property: MaxLength(200)] string? Website);
