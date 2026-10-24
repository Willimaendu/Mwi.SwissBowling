using MudBlazor;
using SwissBowling.Core.Auth;

namespace SwissBowling.Client.Features.Auth.Admin;

public partial class UsersAdmin
{
    private List<UserDto> _users = [];

    protected override Task OnInitializedAsync() => ReloadAsync();

    private Task ReloadAsync() => LoadAsync(async () => _users = await Api.GetAsync<List<UserDto>>(AuthRoutes.Users));

    private async Task CreateAsync()
    {
        if (await ShowDialogAsync<UserDialog, UserDto>("Neuer Benutzer", new DialogParameters<UserDialog>()) is not null)
        {
            await ReloadAsync();
        }
    }

    private Task SetPasswordAsync(UserDto user) =>
        ShowDialogAsync<UserDialog, UserDto>($"Passwort für {user.Email}", new DialogParameters<UserDialog> { { d => d.User, user } }, MaxWidth.ExtraSmall);

    private async Task DeleteAsync(UserDto user)
    {
        if (await ConfirmAsync("Benutzer löschen", $"Soll {user.Email} gelöscht werden?")
            && await TryAsync(() => Api.DeleteAsync(AuthRoutes.User(user.Id)), "Benutzer gelöscht."))
        {
            await ReloadAsync();
        }
    }
}
