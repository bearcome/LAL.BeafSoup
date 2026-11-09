using Microsoft.Extensions.Options;

namespace BeafSoup.Learning;

public sealed class LearningProgressQuery : ILearningProgressQuery
{
    private readonly ILearningActivityRepository _activities;
    private readonly ILearnerProgressStateRepository _states;
    private readonly LearningGamificationOptions _options;
    private readonly TimeProvider _timeProvider;

    public LearningProgressQuery(
        ILearningActivityRepository activities,
        ILearnerProgressStateRepository states,
        IOptions<LearningGamificationOptions> options,
        TimeProvider timeProvider)
    {
        _activities = activities;
        _states = states;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<ProgressSnapshotDto> GetProgressAsync(Guid learnerProfileId, CancellationToken cancellationToken)
    {
        var activities = await _activities.GetByProfileAsync(learnerProfileId, cancellationToken);
        var state = await _states.FindAsync(learnerProfileId, cancellationToken);
        return ProgressSnapshotFactory.Create(
            learnerProfileId, activities, state, _options, _timeProvider.GetUtcNow().UtcDateTime);
    }
}

public sealed class LearningApplicationModule
{
    // 类型锚点：宿主按程序集扫描注册时可引用
}
