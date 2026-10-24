using Microsoft.AspNetCore.Components;
using SwissBowling.Client.Shared.Services;

namespace SwissBowling.Client.Shared.Layout;

public partial class AdminLayout
{
    [Inject] private JwtAuthenticationStateProvider Auth { get; set; } = null!;

    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private bool _drawerOpen = true;
    private bool _darkMode;

    private async Task LogoutAsync()
    {
        await Auth.LogoutAsync();
        Navigation.NavigateTo("/");
    }
}
