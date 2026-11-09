namespace BeafSoup.Blazor.Theming;

/// <summary>用户主题偏好：跟随系统 / 亮 / 暗；持久化在学习配置（服务端），本地仅存可丢弃缓存。</summary>
public enum ThemePreference { System, Light, Dark }

/// <summary>解析后的实际主题。</summary>
public enum Theme { Light, Dark }

public interface IThemeService
{
    ThemePreference Preference { get; }
    Theme Current { get; }

    /// <summary>主题或偏好变化通知（UI 订阅刷新）。</summary>
    event Action? Changed;

    Task InitializeAsync(CancellationToken cancellationToken);
    Task SetPreferenceAsync(ThemePreference preference, CancellationToken cancellationToken);
}
