using Microsoft.AspNetCore.Components;

namespace Mwi.SwissBowling.Web.Components.Pages;

public partial class Administration : ComponentBase
{
    [Parameter] public string? View { get; set; }
    [Inject] private IHttpContextAccessor Context { get; set; } = default!;
}
