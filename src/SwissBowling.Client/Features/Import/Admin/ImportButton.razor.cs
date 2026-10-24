using Microsoft.AspNetCore.Components;

namespace SwissBowling.Client.Features.Import.Admin;

public partial class ImportButton
{
    [Parameter] public bool IsImported { get; set; }

    [Parameter] public bool Busy { get; set; }

    [Parameter] public bool Disabled { get; set; }

    [Parameter] public EventCallback OnClick { get; set; }
}
