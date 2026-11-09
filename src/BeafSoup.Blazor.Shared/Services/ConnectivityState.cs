namespace BeafSoup.Blazor.Shared.Services;

public sealed class ConnectivityState : IConnectivityState
{
    public bool ApiAvailable { get; private set; } = true;
    public event Action? Changed;

    public void ReportUnavailable()
    {
        if (ApiAvailable)
        {
            ApiAvailable = false;
            Changed?.Invoke();
        }
    }

    public void ReportAvailable()
    {
        if (!ApiAvailable)
        {
            ApiAvailable = true;
            Changed?.Invoke();
        }
    }
}
