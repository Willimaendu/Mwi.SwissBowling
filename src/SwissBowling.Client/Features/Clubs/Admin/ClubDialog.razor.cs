using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Clubs;

namespace SwissBowling.Client.Features.Clubs.Admin;

public partial class ClubDialog
{
    [Parameter] public ClubDto? Club { get; set; }

    private string _name = string.Empty;
    private string _shortName = string.Empty;
    private string? _city;

    protected override void OnInitialized() => (_name, _shortName, _city) = (Club?.Name ?? string.Empty, Club?.ShortName ?? string.Empty, Club?.City);

    private Task SaveAsync() => SubmitAsync(
        () =>
        {
            var request = new SaveClubRequest(_name, _shortName, _city);
            return Club is { } c
                ? Api.PutAsync<ClubDto>(ClubRoutes.ById(c.Id), request)
                : Api.PostAsync<ClubDto>(ClubRoutes.Base, request);
        },
        "Klub gespeichert.");
}
