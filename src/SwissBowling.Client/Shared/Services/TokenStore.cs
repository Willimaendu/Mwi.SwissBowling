using Microsoft.JSInterop;

namespace SwissBowling.Client.Shared.Services;

public interface ITokenStore
{
    ValueTask<string?> GetAsync();

    ValueTask SetAsync(string? token);
}

/// <summary>Hält das JWT im Speicher und im localStorage des Browsers (per JS-Interop, daher auch im Interactive-Server-Modus nutzbar).</summary>
public sealed class TokenStore(IJSRuntime js) : ITokenStore
{
    private const string Key = "swissbowling.token";
    private string? _token;
    private bool _loaded;

    public async ValueTask<string?> GetAsync()
    {
        if (_loaded)
        {
            return _token;
        }

        try
        {
            _token = await js.InvokeAsync<string?>("localStorage.getItem", Key);
            _loaded = true;
        }
        catch (InvalidOperationException)
        {
            // Prerendering: kein JS-Interop verfügbar – anonym bleiben und später erneut lesen.
        }
        catch (JSException)
        {
            _loaded = true;
        }

        return _token;
    }

    public async ValueTask SetAsync(string? token)
    {
        _token = token;
        _loaded = true;
        try
        {
            if (token is null)
            {
                await js.InvokeVoidAsync("localStorage.removeItem", Key);
            }
            else
            {
                await js.InvokeVoidAsync("localStorage.setItem", Key, token);
            }
        }
        catch (JSException)
        {
            // Speicher blockiert (z.B. privater Modus): Token bleibt nur im Speicher.
        }
    }
}
