using BeafSoup.Identity;
using BeafSoup.Learning;

namespace BeafSoup.Blazor.Shared.Services;

/// <summary>应用状态：当前学习配置与进度快照；登录后初始化，进度变更统一经此刷新（各组件不自行推导）。</summary>
public class AppState
{
    private readonly ILearningProfileAppService _profiles;
    private readonly ILearningProgressQuery _progress;
    private Task? _profileInitialization;

    public AppState(ILearningProfileAppService profiles, ILearningProgressQuery progress)
        => (_profiles, _progress) = (profiles, progress);

    public LearnerProfileDto? CurrentProfile { get; private set; }
    public ProgressSnapshotDto? Progress { get; private set; }
    public event Action? Changed;

    public async Task EnsureProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (CurrentProfile is not null)
            return;
        // 布局与页面可能同时进入初始化：共享同一任务，避免重复创建配置与竞态回调
        var initialization = _profileInitialization ?? InitializeProfileAsync(userId, cancellationToken);
        _profileInitialization = initialization;
        try
        {
            await initialization;
        }
        catch
        {
            // 失败不能固化为永久缓存；下一次进入页面应能重试
            if (ReferenceEquals(_profileInitialization, initialization))
                _profileInitialization = null;
            throw;
        }
    }

    private async Task InitializeProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var mine = await _profiles.GetMineAsync(userId, cancellationToken);
        CurrentProfile ??= mine.FirstOrDefault();
        if (CurrentProfile is null)
        {
            // 新账户尚无学习配置：按首个里程碑（中文学德语）自动创建，保证学习闭环开箱可用
            CurrentProfile = await _profiles.CreateAsync(
                userId, new CreateLearnerProfileRequest("zh-CN", "de-DE"), cancellationToken);
        }
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
        _profileInitialization = null;
        Changed?.Invoke();
    }
}
