using Microsoft.AspNetCore.Components;
using MudBlazor;
using SwissBowling.Core.Championships;
using SwissBowling.Core.Seasons;

namespace SwissBowling.Client.Features.Championships.Admin;

public partial class ChampionshipAdminDetail
{
    [Parameter] public Guid Id { get; set; }

    private ChampionshipDetailDto? _detail;

    protected override Task OnParametersSetAsync() => ReloadAsync();

    private Task ReloadAsync() => LoadAsync(async () => _detail = await Api.GetAsync<ChampionshipDetailDto>(ChampionshipRoutes.ById(Id)));

    private async Task EditAsync()
    {
        var seasons = await Api.GetAsync<List<SeasonDto>>(SeasonRoutes.Base);
        var parameters = new DialogParameters<ChampionshipDialog> { { d => d.Championship, _detail!.Championship }, { d => d.Seasons, seasons } };
        if (await ShowDialogAsync<ChampionshipDialog, ChampionshipDto>("Meisterschaft bearbeiten", parameters) is not null)
        {
            await ReloadAsync();
        }
    }

    private async Task DeleteAsync()
    {
        if (await ConfirmAsync("Meisterschaft löschen", $"Soll «{_detail!.Championship.Name}» gelöscht werden?")
            && await TryAsync(() => Api.DeleteAsync(ChampionshipRoutes.ById(Id)), "Meisterschaft gelöscht."))
        {
            Navigation.NavigateTo("/admin/meisterschaften");
        }
    }

    private async Task EditRoundAsync(RoundDto? round)
    {
        var parameters = new DialogParameters<RoundDialog> { { d => d.ChampionshipId, Id }, { d => d.Round, round } };
        if (await ShowDialogAsync<RoundDialog, RoundDto>(round is null ? "Neue Runde" : $"Runde {round.Number} bearbeiten", parameters, MaxWidth.ExtraSmall) is not null)
        {
            await ReloadAsync();
        }
    }

    private async Task DeleteRoundAsync(RoundDto round)
    {
        if (await ConfirmAsync("Runde löschen", $"Soll Runde {round.Number} gelöscht werden?")
            && await TryAsync(() => Api.DeleteAsync(ChampionshipRoutes.Round(round.Id)), "Runde gelöscht."))
        {
            await ReloadAsync();
        }
    }
}
