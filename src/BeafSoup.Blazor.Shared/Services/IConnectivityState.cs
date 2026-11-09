namespace BeafSoup.Blazor.Shared.Services;

/// <summary>API 连通状态：服务端不可用时明确报告，不回退为伪造成功或伪统计（验收场景）。</summary>
public interface IConnectivityState
{
    bool ApiAvailable { get; }
    event Action? Changed;
    void ReportUnavailable();
    void ReportAvailable();
}
