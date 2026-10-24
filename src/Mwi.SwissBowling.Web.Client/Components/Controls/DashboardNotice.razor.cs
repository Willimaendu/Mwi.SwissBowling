using Microsoft.AspNetCore.Components;

namespace Mwi.SwissBowling.Web.Client.Components.Controls;

public partial class DashboardNotice : ComponentBase
{
    [Parameter] public bool IsError { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
