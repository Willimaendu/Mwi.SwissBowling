using Microsoft.AspNetCore.Components;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Web.Client.Infrastructure;

namespace Mwi.SwissBowling.Web.Client.Features.Administration;

public abstract class SetupFormBase : ComponentBase
{
    [Inject] protected IAdminApi Api { get; set; } = default!;
    [Parameter, EditorRequired] public SetupDto Data { get; set; } = default!;
    [Parameter, EditorRequired] public Func<Func<Task>, Task> Execute { get; set; } = default!;
    [Parameter] public bool Busy { get; set; }
    protected Task CreateAsync(string path, object input) => Execute(() => Api.PostAsync("setup/" + path, input));
    protected static string? Optional(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
