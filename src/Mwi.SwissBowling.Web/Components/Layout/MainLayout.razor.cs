using Microsoft.AspNetCore.Components;

namespace Mwi.SwissBowling.Web.Components.Layout;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private string WorkspaceTitle => new Uri(Navigation.Uri).AbsolutePath.TrimEnd('/').ToLowerInvariant() switch
    {
        "/administration" => "Übersicht",
        "/administration/results" => "Resultaterfassung",
        "/administration/setup" => "Stammdaten",
        "/administration/approvals" => "Freigaben",
        _ => "Meisterschaften"
    };
}
