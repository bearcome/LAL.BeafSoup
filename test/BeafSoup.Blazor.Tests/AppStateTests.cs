using BeafSoup.Blazor.Shared.Services;
using BeafSoup.Learning;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Blazor.Tests;

public sealed class AppStateTests
{
    [Fact]
    public async Task EnsureProfileAsync_shares_concurrent_initialization()
    {
        var profiles = new DeferredProfileService();
        var state = new AppState(profiles, new StubProgressQuery());
        var userId = Guid.NewGuid();

        var first = state.EnsureProfileAsync(userId, CancellationToken.None);
        var second = state.EnsureProfileAsync(userId, CancellationToken.None);
        profiles.Release();
        await Task.WhenAll(first, second);

        profiles.GetMineCalls.Should().Be(1);
        state.CurrentProfile.Should().NotBeNull();
        state.Progress.Should().NotBeNull();
    }

    [Fact]
    public async Task EnsureProfileAsync_allows_retry_after_failure()
    {
        var profiles = new DeferredProfileService { FailNextRead = true };
        var state = new AppState(profiles, new StubProgressQuery());

        await FluentActions.Awaiting(() => state.EnsureProfileAsync(Guid.NewGuid(), CancellationToken.None))
            .Should().ThrowAsync<HttpRequestException>();
        profiles.Release();
        await state.EnsureProfileAsync(Guid.NewGuid(), CancellationToken.None);

        profiles.GetMineCalls.Should().Be(2);
        state.CurrentProfile.Should().NotBeNull();
    }

    private sealed class DeferredProfileService : ILearningProfileAppService
    {
        private TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int GetMineCalls { get; private set; }
        public bool FailNextRead { get; init; }
        private bool _hasFailed;

        public void Release() => _gate.TrySetResult();

        public async Task<IReadOnlyList<LearnerProfileDto>> GetMineAsync(
            Guid userId, CancellationToken cancellationToken)
        {
            GetMineCalls++;
            if (FailNextRead && !_hasFailed)
            {
                _hasFailed = true;
                throw new HttpRequestException("Test failure");
            }
            await _gate.Task.WaitAsync(cancellationToken);
            return [new LearnerProfileDto(
                Guid.NewGuid(), "zh-CN", "de-DE", 0.9, "system", "All", "Click")];
        }

        public Task<LearnerProfileDto> CreateAsync(
            Guid userId, CreateLearnerProfileRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<LearnerProfileDto> UpdatePreferencesAsync(
            Guid profileId, UpdateProfilePreferencesRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class StubProgressQuery : ILearningProgressQuery
    {
        public Task<ProgressSnapshotDto> GetProgressAsync(
            Guid learnerProfileId, CancellationToken cancellationToken)
            => Task.FromResult(new ProgressSnapshotDto(learnerProfileId, 0, 0, 5, 0, false));
    }
}
