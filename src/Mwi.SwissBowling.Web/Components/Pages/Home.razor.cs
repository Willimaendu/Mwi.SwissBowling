using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Web.Features.Standings;

namespace Mwi.SwissBowling.Web.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject] private PublicEventsClient Events { get; set; } = default!;
    [Inject] private IHttpContextAccessor Context { get; set; } = default!;
    [Parameter] public IComponentRenderMode? LiveRenderMode { get; set; } = RenderMode.InteractiveWebAssembly;

    private PublicEventSummary[]? _events;
    private bool _error;

    protected override async Task OnInitializedAsync()
    {
        try { _events = await Events.ListAsync(); }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _error = true;
            if (Context.HttpContext?.Response.HasStarted == false) Context.HttpContext.Response.StatusCode = 503;
        }
    }
}
