using Microsoft.JSInterop;

namespace Privestio.Web.Services;

public interface IConnectivityService
{
    bool IsOnline { get; }
    event Action<bool>? ConnectivityChanged;
    Task InitializeAsync();
}

public class ConnectivityService : IConnectivityService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<ConnectivityService>? _dotNetRef;

    public bool IsOnline { get; private set; } = true;
    public event Action<bool>? ConnectivityChanged;

    public ConnectivityService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        var online = await _jsRuntime.InvokeAsync<bool>(
            "connectivityFunctions.initialize",
            _dotNetRef
        );
        IsOnline = online;
    }

    [JSInvokable]
    public void OnConnectivityChanged(bool isOnline)
    {
        IsOnline = isOnline;
        ConnectivityChanged?.Invoke(isOnline);
    }

    public async ValueTask DisposeAsync()
    {
        if (_dotNetRef != null)
        {
            await _jsRuntime.InvokeVoidAsync("connectivityFunctions.dispose");
            _dotNetRef.Dispose();
            _dotNetRef = null;
        }
    }
}
