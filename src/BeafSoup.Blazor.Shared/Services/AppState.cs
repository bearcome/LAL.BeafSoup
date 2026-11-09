using BeafSoup.Identity;
using BeafSoup.Learning;

namespace BeafSoup.Blazor.Shared.Services;

/// <summary>应用状态：当前学习配置与进度快照；登录后初始化，进度变更统一经此刷新（各组件不自行推导）。</summary>
public class AppState
{
    private readonly ILearningProfileAppService _profiles;
    private readonly ILearningProgressQuery _progress;

    public AppState(ILearningProfileAppService profiles, ILearningProgressQuery progress)
        => (_profiles, _progress) = (profiles, progress);

    public LearnerProfileDto? CurrentProfile { get; private set; }
    public ProgressSnapshotDto? Progress { get; private set; }
    public event Action? Changed;

    public async Task EnsureProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (CurrentProfile is not null)
            return;
        var mine = await _profiles.GetMineAsync(userId, cancellationToken);
        CurrentProfile ??= mine.FirstOrDefault();
        if (CurrentProfile is not null)
            await RefreshProgressAsync(cancellationToken);
        Changed?.Invoke();
    }

    public async Task RefreshProgressAsync(CancellationToken cancellationToken)
    {
        if (CurrentProfile is null)
            return;
        Progress = await _progress.GetProgressAsync(CurrentProfile.Id, cancellationToken);
        Changed?.Invoke();
    }

    public async Task UpdateThemePreferenceAsync(string theme, CancellationToken cancellationToken)
    {
        if (CurrentProfile is null)
            return;
        CurrentProfile = await _profiles.UpdatePreferencesAsync(
            CurrentProfile.Id, new UpdateProfilePreferencesRequest(theme, null, null, null), cancellationToken);
        Changed?.Invoke();
    }

    public void Reset()
    {
        CurrentProfile = null;
        Progress = null;
        Changed?.Invoke();
    }
}
