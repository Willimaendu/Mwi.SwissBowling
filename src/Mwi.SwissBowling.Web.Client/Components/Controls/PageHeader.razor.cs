using Microsoft.AspNetCore.Components;

namespace Mwi.SwissBowling.Web.Client.Components.Controls;

public partial class PageHeader : ComponentBase
{
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter] public string? Description { get; set; }
    [Parameter] public RenderFragment? Actions { get; set; }
}
