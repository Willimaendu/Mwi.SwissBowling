using MudBlazor;
using SwissBowling.Core.Associations;
using SwissBowling.Core.Championships;

namespace SwissBowling.Client.Features.Championships.Admin;

public partial class ChampionshipsAdmin
{
    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.Today);
    private List<AssociationDto> _associations = [];
    private List<ChampionshipDto> _championships = [];

    protected override Task OnInitializedAsync() => LoadAsync(async () =>
    {
        _associations = await Api.GetAsync<List<AssociationDto>>(AssociationRoutes.Base);
        _championships = await Api.GetAsync<List<ChampionshipDto>>(ChampionshipRoutes.Base);
    });

    private async Task CreateAsync()
    {
        var parameters = new DialogParameters<ChampionshipDialog> { { d => d.Associations, _associations } };
        if (await ShowDialogAsync<ChampionshipDialog, ChampionshipDto>("Neue Meisterschaft", parameters) is { } created)
        {
            Navigation.NavigateTo($"/admin/meisterschaften/{created.Id}");
        }
    }
}
