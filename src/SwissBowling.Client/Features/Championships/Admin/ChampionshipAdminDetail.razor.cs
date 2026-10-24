using Microsoft.AspNetCore.Components;
using MudBlazor;
using SwissBowling.Core.Associations;
using SwissBowling.Core.Championships;

namespace SwissBowling.Client.Features.Championships.Admin;

public partial class ChampionshipAdminDetail
{
    [Parameter] public Guid Id { get; set; }

    private ChampionshipDetailDto? _detail;

    protected override Task OnParametersSetAsync() => ReloadAsync();

    private Task ReloadAsync() => LoadAsync(async () => _detail = await Api.GetAsync<ChampionshipDetailDto>(ChampionshipRoutes.ById(Id)));

    private async Task EditAsync()
    {
        var associations = await Api.GetAsync<List<AssociationDto>>(AssociationRoutes.Base);
        var parameters = new DialogParameters<ChampionshipDialog> { { d => d.Championship, _detail!.Championship }, { d => d.Associations, associations } };
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
}
