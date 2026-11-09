using Microsoft.JSInterop;

namespace BeafSoup.Blazor.Theming;

/// <summary>主题服务：默认跟随系统，手动开关常驻可见；加载时经内联脚本预置 data-theme，无闪烁；JS 不可用（如预渲染）时静默降级。</summary>
public sealed class ThemeService : IThemeService, IAsyncDisposable
{
    private const string StorageKey = "beafsoup.theme-preference";
    private readonly IJSRuntime _jsRuntime;
    private readonly Lazy<Task<IJSObjectReference>> _module;
    private bool _initialized;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _module = new Lazy<Task<IJSObjectReference>>(() =>
            _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/BeafSoup.Blazor.Theming/theme.js").AsTask());
    }

    public ThemePreference Preference { get; private set; } = ThemePreference.System;
    public Theme Current { get; private set; } = Theme.Light;
    public event Action? Changed;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
            return;
        try
        {
            var module = await _module.Value;
            var stored = await module.InvokeAsync<string?>("getStoredPreference", cancellationToken, StorageKey);
            Preference = Enum.TryParse<ThemePreference>(stored, out var preference) ? preference : ThemePreference.System;
            await ApplyAsync(cancellationToken);
            // 系统主题切换实时跟随（仅偏好为 System 时生效）
            var self = DotNetObjectReference.Create(this);
            await module.InvokeVoidAsync("subscribeSystemTheme", cancellationToken, self);
            _initialized = true;
        }
        catch (InvalidOperationException)
        {
            // JS 运行时尚未可用（预渲染等）：保持默认，后续 SetPreference 会重试
        }
    }

    public async Task SetPreferenceAsync(ThemePreference preference, CancellationToken cancellationToken)
    {
        Preference = preference;
        try
        {
            var module = await _module.Value;
            await module.InvokeVoidAsync("storePreference", cancellationToken, StorageKey, preference.ToString());
            await ApplyAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // JS 不可用：仅保留内存偏好
        }
        Changed?.Invoke();
    }

    /// <summary>JS 回调：系统主题变化时重新解析（偏好为 System 才响应）。</summary>
    [JSInvokable]
    public async Task OnSystemThemeChanged()
    {
        if (Preference is not ThemePreference.System)
            return;
        await ApplyAsync(CancellationToken.None);
        Changed?.Invoke();
    }

    private async Task ApplyAsync(CancellationToken cancellationToken)
    {
        try
        {
            var module = await _module.Value;
            var themeName = Preference switch
            {
                ThemePreference.Light => "light",
                ThemePreference.Dark => "dark",
                _ => await module.InvokeAsync<string>("getSystemTheme", cancellationToken),
            };
            Current = themeName == "dark" ? Theme.Dark : Theme.Light;
            await module.InvokeVoidAsync("applyTheme", cancellationToken, themeName);
        }
        catch (InvalidOperationException)
        {
            // JS 不可用：保持现有主题
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module.IsValueCreated)
        {
            var module = await _module.Value;
            await module.DisposeAsync();
        }
    }
}
