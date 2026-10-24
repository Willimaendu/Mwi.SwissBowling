using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Associations;

namespace SwissBowling.Client.Features.Associations.Admin;

public partial class AssociationDialog
{
    [Parameter] public AssociationDto? Association { get; set; }

    private string _name = string.Empty;
    private string? _shortCode;
    private string? _website;

    protected override void OnInitialized() => (_name, _shortCode, _website) = (Association?.Name ?? string.Empty, Association?.ShortCode, Association?.Website);

    private Task SaveAsync() => SubmitAsync(
        () =>
        {
            var request = new SaveAssociationRequest(_name, _shortCode, _website);
            return Association is { } a
                ? Api.PutAsync<AssociationDto>(AssociationRoutes.ById(a.Id), request)
                : Api.PostAsync<AssociationDto>(AssociationRoutes.Base, request);
        },
        "Unterverband gespeichert.");
}
