using Microsoft.AspNetCore.Components;
using MudBlazor;
using SwissBowling.Client.Shared.Services;

namespace SwissBowling.Client.Shared.Components;

/// <summary>Basis für Bearbeitungsdialoge: Validierung, Speichern über die API, Fehleranzeige.</summary>
public abstract class FormDialog : ComponentBase
{
    [CascadingParameter] protected IMudDialogInstance Dialog { get; set; } = null!;

    [Inject] protected IApiClient Api { get; set; } = null!;

    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    protected MudForm? Form { get; set; }

    protected bool IsValid { get; set; }

    protected bool Busy { get; private set; }

    protected string? Error { get; private set; }

    protected void Cancel() => Dialog.Cancel();

    /// <summary>Validiert das Formular, führt <paramref name="save"/> aus und schliesst den Dialog mit dem Ergebnis.</summary>
    protected async Task SubmitAsync<T>(Func<Task<T>> save, string successMessage)
    {
        if (Form is not null)
        {
            await Form.Validate();
            if (!Form.IsValid)
            {
                return;
            }
        }

        Busy = true;
        Error = null;
        try
        {
            var result = await save();
            Snackbar.Add(successMessage, Severity.Success);
            Dialog.Close(DialogResult.Ok(result));
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
        finally
        {
            Busy = false;
        }
    }
}
