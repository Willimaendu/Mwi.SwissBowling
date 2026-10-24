using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Associations;
using SwissBowling.Core.Clubs;

namespace SwissBowling.Client.Features.Clubs.Admin;

public partial class ClubDialog
{
    [Parameter] public ClubDto? Club { get; set; }

    [Parameter] public List<AssociationDto> Associations { get; set; } = [];

    private string _name = string.Empty;
    private string? _city;
    private Guid _associationId;

    protected override void OnInitialized() =>
        (_name, _city, _associationId) = (Club?.Name ?? string.Empty, Club?.City, Club?.AssociationId ?? Associations.FirstOrDefault()?.Id ?? Guid.Empty);

    private Task SaveAsync() => SubmitAsync(
        () =>
        {
            var request = new SaveClubRequest(_name, _city, _associationId);
            return Club is { } c
                ? Api.PutAsync<ClubDto>(ClubRoutes.ById(c.Id), request)
                : Api.PostAsync<ClubDto>(ClubRoutes.Base, request);
        },
        "Klub gespeichert.");
}
