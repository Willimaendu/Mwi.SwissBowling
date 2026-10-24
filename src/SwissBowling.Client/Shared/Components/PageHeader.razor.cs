using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace SwissBowling.Client.Shared.Components;

public partial class PageHeader
{
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    [Parameter] public string? Subtitle { get; set; }

    [Parameter] public RenderFragment? Actions { get; set; }

    [Parameter] public List<BreadcrumbItem>? Breadcrumbs { get; set; }
}
