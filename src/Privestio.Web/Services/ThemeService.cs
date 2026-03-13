using Microsoft.JSInterop;

namespace Privestio.Web.Services;

public interface IThemeService
{
    ThemeState State { get; }
    event Action<ThemeState>? ThemeChanged;
    Task InitializeAsync();
    Task SetPreferenceAsync(string preference);
}

public sealed class ThemeService : IThemeService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<ThemeService>? _dotNetRef;

    public ThemeState State { get; private set; } = ThemeState.Default;
    public event Action<ThemeState>? ThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (_dotNetRef is not null)
            return;

        _dotNetRef = DotNetObjectReference.Create(this);
        State = await _jsRuntime.InvokeAsync<ThemeState>("themeFunctions.initialize", _dotNetRef);
        ThemeChanged?.Invoke(State);
    }

    public async Task SetPreferenceAsync(string preference)
    {
        State = await _jsRuntime.InvokeAsync<ThemeState>("themeFunctions.setPreference", preference);
        ThemeChanged?.Invoke(State);
    }

    [JSInvokable]
    public void OnThemeChanged(ThemeState state)
    {
        State = state ?? ThemeState.Default;
        ThemeChanged?.Invoke(State);
    }

    public async ValueTask DisposeAsync()
    {
        if (_dotNetRef is null)
            return;

        await _jsRuntime.InvokeVoidAsync("themeFunctions.dispose");
        _dotNetRef.Dispose();
        _dotNetRef = null;
    }
}

public sealed record ThemeState
{
    public static ThemeState Default { get; } = new();

    public string Preference { get; init; } = "system";
    public string ResolvedTheme { get; init; } = "light";
}