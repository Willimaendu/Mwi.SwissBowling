using Microsoft.AspNetCore.Components;
using SwissBowling.Client.Shared.Services;

namespace SwissBowling.Client.Shared.Components;

public partial class ActivityBar : IDisposable
{
    [Inject] private ApiActivity Activity { get; set; } = null!;

    protected override void OnInitialized() => Activity.Changed += OnChanged;

    private void OnChanged() => _ = InvokeAsync(StateHasChanged);

    public void Dispose() => Activity.Changed -= OnChanged;
}
