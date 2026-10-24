using Microsoft.AspNetCore.Components;

namespace SwissBowling.Client.Shared.Components;

public partial class RedirectToLogin
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && RendererInfo.IsInteractive)
        {
            var returnUrl = Uri.EscapeDataString("/" + Navigation.ToBaseRelativePath(Navigation.Uri));
            Navigation.NavigateTo($"/login?returnUrl={returnUrl}");
        }
    }
}
