using System.ComponentModel.DataAnnotations;

namespace SwissBowling.Core.Associations;

public static class AssociationRoutes
{
    public const string Base = "api/associations";

    public static string ById(Guid id) => $"{Base}/{id}";
}

/// <summary>Unterverband (UV) der SFKV, z.B. Emmental, Bern-Stadt.</summary>
public sealed record AssociationDto(Guid Id, string Name, int? ExternalId, int ClubCount);

public sealed record SaveAssociationRequest([property: Required, MaxLength(100)] string Name);
