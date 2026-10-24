namespace SwissBowling.Client.Shared.Services;

/// <summary>Zählt laufende API-Anfragen, damit die Layouts einen globalen Fortschrittsbalken anzeigen können.</summary>
public sealed class ApiActivity
{
    private int _pending;

    public bool IsBusy => _pending > 0;

    public event Action? Changed;

    public IDisposable Begin()
    {
        Interlocked.Increment(ref _pending);
        Changed?.Invoke();
        return new Scope(this);
    }

    private void End()
    {
        Interlocked.Decrement(ref _pending);
        Changed?.Invoke();
    }

    private sealed class Scope(ApiActivity owner) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                owner.End();
            }
        }
    }
}
