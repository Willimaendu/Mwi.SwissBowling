using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Associations;

namespace SwissBowling.Client.Features.Associations.Admin;

public partial class AssociationDialog
{
    [Parameter] public AssociationDto? Association { get; set; }

    private string _name = string.Empty;

    protected override void OnInitialized() => _name = Association?.Name ?? string.Empty;

    private Task SaveAsync() => SubmitAsync(
        () => Association is { } a
            ? Api.PutAsync<AssociationDto>(AssociationRoutes.ById(a.Id), new SaveAssociationRequest(_name))
            : Api.PostAsync<AssociationDto>(AssociationRoutes.Base, new SaveAssociationRequest(_name)),
        "Unterverband gespeichert.");
}
