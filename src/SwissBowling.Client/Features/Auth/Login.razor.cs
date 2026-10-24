using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using SwissBowling.Client.Shared.Services;
using SwissBowling.Core.Auth;

namespace SwissBowling.Client.Features.Auth;

public partial class Login
{
    [Inject] private JwtAuthenticationStateProvider Auth { get; set; } = null!;

    [Inject] private NavigationManager Navigation { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    private MudForm? _form;
    private bool _isValid;
    private bool _busy;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string? _error;

    private Task OnKeyDown(KeyboardEventArgs args) => args.Key == "Enter" && _isValid ? LoginAsync() : Task.CompletedTask;

    private async Task LoginAsync()
    {
        if (_form is not null)
        {
            await _form.Validate();
            if (!_form.IsValid)
            {
                return;
            }
        }

        _busy = true;
        _error = null;
        try
        {
            await Auth.LoginAsync(new LoginRequest(_email.Trim(), _password));
            Navigation.NavigateTo(string.IsNullOrWhiteSpace(ReturnUrl) || ReturnUrl.Contains("/login", StringComparison.OrdinalIgnoreCase) ? "/admin" : ReturnUrl);
        }
        catch (ApiException ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }
}
