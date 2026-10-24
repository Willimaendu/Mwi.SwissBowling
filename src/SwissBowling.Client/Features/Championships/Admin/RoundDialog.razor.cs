using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Championships;

namespace SwissBowling.Client.Features.Championships.Admin;

public partial class RoundDialog
{
    [Parameter] public Guid ChampionshipId { get; set; }

    [Parameter] public RoundDto? Round { get; set; }

    private DateTime? _date = DateTime.Today;
    private string _venue = string.Empty;

    protected override void OnInitialized()
    {
        if (Round is { } r)
        {
            _date = r.Date.ToDateTime(TimeOnly.MinValue);
            _venue = r.Venue;
        }
    }

    private Task SaveAsync() => SubmitAsync(
        () =>
        {
            var request = new SaveRoundRequest(DateOnly.FromDateTime(_date!.Value), _venue);
            return Round is { } r
                ? Api.PutAsync<RoundDto>(ChampionshipRoutes.Round(r.Id), request)
                : Api.PostAsync<RoundDto>(ChampionshipRoutes.Rounds(ChampionshipId), request);
        },
        "Runde gespeichert.");
}
