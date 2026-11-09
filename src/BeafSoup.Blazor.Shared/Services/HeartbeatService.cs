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
        using var timer = new PeriodicTimer(_interval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var tokens = _tokens.Tokens;
                if (tokens is null || _tokens.IsExpired)
                {
                    Stop();
                    SessionExpired?.Invoke();
                    return;
                }
                try
                {
                    await _sessions.RecordHeartbeatAsync(tokens.SessionId, cancellationToken);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // 会话被顶替或已失效：停止心跳并通知登出
                    Stop();
                    SessionExpired?.Invoke();
                    return;
                }
                catch (HttpRequestException)
                {
                    _connectivity.ReportUnavailable();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常停止
        }
    }

    public ValueTask DisposeAsync()
    {
        Stop();
        return ValueTask.CompletedTask;
    }
}
