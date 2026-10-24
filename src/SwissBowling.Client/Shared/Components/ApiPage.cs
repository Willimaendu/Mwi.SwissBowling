using Microsoft.AspNetCore.Components;
using MudBlazor;
using SwissBowling.Client.Shared.Services;

namespace SwissBowling.Client.Shared.Components;

/// <summary>Basis für Seiten mit API-Zugriff: einheitliches Laden, Fehlerbehandlung und Rückmeldungen.</summary>
public abstract class ApiPage : ComponentBase
{
    [Inject] protected IApiClient Api { get; set; } = null!;

    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    [Inject] protected IDialogService Dialogs { get; set; } = null!;

    [Inject] protected NavigationManager Navigation { get; set; } = null!;

    protected bool Loading { get; private set; } = true;

    protected string? Error { get; private set; }

    /// <summary>Lädt Daten; Fehler landen in <see cref="Error"/> statt in einer Exception.</summary>
    protected async Task LoadAsync(Func<Task> load)
    {
        Loading = true;
        Error = null;
        try
        {
            await load();
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException)
        {
            Error = ex.Message;
        }
        finally
        {
            Loading = false;
        }
    }

    /// <summary>Führt eine Aktion aus und meldet Erfolg bzw. Fehler per Snackbar.</summary>
    protected async Task<bool> TryAsync(Func<Task> action, string? successMessage = null)
    {
        try
        {
            await action();
            if (successMessage is not null)
            {
                Snackbar.Add(successMessage, Severity.Success);
            }

            return true;
        }
        catch (ApiException ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
            return false;
        }
    }

    protected async Task<bool> ConfirmAsync(string title, string message, string yesText = "Löschen") =>
        await Dialogs.ShowMessageBox(title, message, yesText: yesText, cancelText: "Abbrechen") == true;

    /// <summary>Öffnet einen Dialog und liefert dessen Ergebnis, wenn er nicht abgebrochen wurde.</summary>
    protected async Task<TResult?> ShowDialogAsync<TDialog, TResult>(string title, DialogParameters<TDialog> parameters, MaxWidth width = MaxWidth.Small)
        where TDialog : ComponentBase
        where TResult : class
    {
        var dialog = await Dialogs.ShowAsync<TDialog>(title, parameters, new DialogOptions { MaxWidth = width, FullWidth = true });
        var result = await dialog.Result;
        return result is { Canceled: false, Data: TResult data } ? data : null;
    }
}
