using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Seasons;

namespace SwissBowling.Client.Features.Seasons.Admin;

public partial class SeasonDialog
{
    [Parameter] public SeasonDto? Season { get; set; }

    private string _name = string.Empty;
    private DateTime? _start;
    private DateTime? _end;

    protected override void OnInitialized()
    {
        if (Season is { } s)
        {
            (_name, _start, _end) = (s.Name, s.StartDate.ToDateTime(TimeOnly.MinValue), s.EndDate.ToDateTime(TimeOnly.MinValue));
            return;
        }

        var year = DateTime.Today.Month >= 7 ? DateTime.Today.Year : DateTime.Today.Year - 1;
        (_name, _start, _end) = ($"{year}/{(year + 1) % 100:00}", new DateTime(year, 9, 1), new DateTime(year + 1, 6, 30));
    }

    private Task SaveAsync() => SubmitAsync(
        () =>
        {
            var request = new SaveSeasonRequest(_name, DateOnly.FromDateTime(_start!.Value), DateOnly.FromDateTime(_end!.Value));
            return Season is { } s
                ? Api.PutAsync<SeasonDto>(SeasonRoutes.ById(s.Id), request)
                : Api.PostAsync<SeasonDto>(SeasonRoutes.Base, request);
        },
        "Saison gespeichert.");
}
