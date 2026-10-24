using Microsoft.AspNetCore.Components;

namespace Mwi.SwissBowling.Web.Components.Layout;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private string CurrentWorkspace => new Uri(Navigation.Uri).AbsolutePath.StartsWith("/administration", StringComparison.OrdinalIgnoreCase)
        ? "Administration"
        : "Meisterschaften";
}
