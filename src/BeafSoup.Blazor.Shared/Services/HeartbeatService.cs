using System.Net;
using BeafSoup.Identity;

namespace BeafSoup.Blazor.Shared.Services;

/// <summary>在线心跳：登录后按周期发送；服务端不可用明确报告；401 视为会话失效（单会话顶替）。</summary>
public sealed class HeartbeatService : IAsyncDisposable
{
    private readonly ISessionAppService _sessions;
    private readonly IConnectivityState _connectivity;
    private readonly IAuthTokenStore _tokens;
    private readonly TimeSpan _interval;
    private CancellationTokenSource? _cts;

    public HeartbeatService(
        ISessionAppService sessions,
        IConnectivityState connectivity,
        IAuthTokenStore tokens,
        TimeSpan? interval = null)
    {
        _sessions = sessions;
        _connectivity = connectivity;
        _tokens = tokens;
        _interval = interval ?? TimeSpan.FromSeconds(30); // ADR-007 客户端提示周期
    }

    public event Action? SessionExpired;

    public void Start()
    {
        if (_cts is not null)
            return;
        _cts = new CancellationTokenSource();
        _ = RunLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        // 启动时立即发送一次：页面刷新/回访后马上续租，避免等到第一个周期才恢复在线状态
        if (!await SendOnceAsync(cancellationToken))
            return;

        using var timer = new PeriodicTimer(_interval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (!await SendOnceAsync(cancellationToken))
                    return;
            }
        }
        catch (OperationCanceledException)
        {
            // 正常停止
        }
    }

    /// <summary>发送一次心跳；令牌失效或服务端 401 时停止并通知登出。返回是否继续循环。</summary>
    private async Task<bool> SendOnceAsync(CancellationToken cancellationToken)
    {
        var tokens = _tokens.Tokens;
        if (tokens is null || _tokens.IsExpired)
        {
            Stop();
            SessionExpired?.Invoke();
            return false;
        }
        try
        {
            await _sessions.RecordHeartbeatAsync(tokens.SessionId, cancellationToken);
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            // 会话被顶替或已失效：停止心跳并通知登出
            Stop();
            SessionExpired?.Invoke();
            return false;
        }
        catch (HttpRequestException)
        {
            _connectivity.ReportUnavailable();
            return true;
        }
    }

    public ValueTask DisposeAsync()
    {
        Stop();
        return ValueTask.CompletedTask;
    }
}
