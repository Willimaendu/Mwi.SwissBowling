using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Associations;

namespace SwissBowling.Client.Features.Associations;

public partial class AssociationDetail
{
    [Parameter] public Guid Id { get; set; }

    private AssociationDetailDto? _detail;

    protected override Task OnParametersSetAsync() => LoadAsync(async () => _detail = await FetchAsync<AssociationDetailDto>($"association-{Id}", AssociationRoutes.ById(Id)));
}
