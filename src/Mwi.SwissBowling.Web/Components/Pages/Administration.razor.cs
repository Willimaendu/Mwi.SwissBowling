using Microsoft.AspNetCore.Components;

namespace Mwi.SwissBowling.Web.Components.Pages;

public partial class Administration : ComponentBase
{
    [Inject] private IHttpContextAccessor Context { get; set; } = default!;
}
