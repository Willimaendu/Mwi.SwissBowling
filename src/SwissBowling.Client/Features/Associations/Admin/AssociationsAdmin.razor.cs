using MudBlazor;
using SwissBowling.Core.Associations;

namespace SwissBowling.Client.Features.Associations.Admin;

public partial class AssociationsAdmin
{
    private List<AssociationDto> _items = [];

    protected override Task OnInitializedAsync() => ReloadAsync();

    private Task ReloadAsync() => LoadAsync(async () => _items = await Api.GetAsync<List<AssociationDto>>(AssociationRoutes.Base));

    private async Task EditAsync(AssociationDto? association)
    {
        var parameters = new DialogParameters<AssociationDialog> { { d => d.Association, association } };
        if (await ShowDialogAsync<AssociationDialog, AssociationDto>(association is null ? "Neuer Unterverband" : "Unterverband bearbeiten", parameters, MaxWidth.ExtraSmall) is not null)
        {
            await ReloadAsync();
        }
    }

    private async Task DeleteAsync(AssociationDto association)
    {
        if (await ConfirmAsync("Unterverband löschen", $"Soll «{association.Name}» gelöscht werden?")
            && await TryAsync(() => Api.DeleteAsync(AssociationRoutes.ById(association.Id)), "Unterverband gelöscht."))
        {
            await ReloadAsync();
        }
    }
}
