using BeafSoup.Blazor.Client;
using BeafSoup.Blazor.Shared.Services;
using BeafSoup.Blazor.Theming;
using BeafSoup.Identity;
using BeafSoup.Content;
using BeafSoup.Exercises;
using BeafSoup.Learning;
using BeafSoup.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 独立静态部署：API 基址来自 wwwroot/appsettings.json（部署时可改）
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("缺少 ApiBaseUrl 配置。");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// 主题（默认跟随系统，无闪烁初始化由 index.html 内联脚本完成）
builder.Services.AddScoped<IThemeService, ThemeService>();

// 认证状态 + 令牌缓存（浏览器存储仅为可丢弃缓存，ADR-004）
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IAuthTokenStore, LocalAuthTokenStore>();
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<AuthStateProvider>());

// 连通性与心跳
builder.Services.AddScoped<IConnectivityState, ConnectivityState>();
builder.Services.AddScoped<HeartbeatService>();

// ApiClient：只实现 Contracts 中定义的客户端接口
builder.Services.AddScoped<IdentityApiClient>();
builder.Services.AddScoped<IIdentityAppService>(provider => provider.GetRequiredService<IdentityApiClient>());
builder.Services.AddScoped<ISessionAppService>(provider => provider.GetRequiredService<IdentityApiClient>());
builder.Services.AddScoped<IAccountLifecycleAppService>(provider => provider.GetRequiredService<IdentityApiClient>());
builder.Services.AddScoped<UiTextApiClient>();
builder.Services.AddScoped<IUiTextCatalogQuery>(provider => provider.GetRequiredService<UiTextApiClient>());

// 第 4 阶段：学习闭环客户端
builder.Services.AddScoped<LearningApiClient>();
builder.Services.AddScoped<ILearningProfileAppService>(provider => provider.GetRequiredService<LearningApiClient>());
builder.Services.AddScoped<ILearningProgressQuery>(provider => provider.GetRequiredService<LearningApiClient>());
builder.Services.AddScoped<IReviewAppService>(provider => provider.GetRequiredService<LearningApiClient>());
builder.Services.AddScoped<ILearningActivityAppService>(provider => provider.GetRequiredService<LearningApiClient>());
builder.Services.AddScoped<ContentApiClient>();
builder.Services.AddScoped<BeafSoup.Content.ICourseCatalogQuery>(provider => provider.GetRequiredService<ContentApiClient>());
builder.Services.AddScoped<BeafSoup.Content.IContentLibraryQuery>(provider => provider.GetRequiredService<ContentApiClient>());
builder.Services.AddScoped<ExerciseApiClient>();
builder.Services.AddScoped<IExercisePresentationQuery>(provider => provider.GetRequiredService<ExerciseApiClient>());
builder.Services.AddScoped<IExerciseGradingAppService>(provider => provider.GetRequiredService<ExerciseApiClient>());
builder.Services.AddScoped<IDictionaryCache>(_ =>
    // 词典是客户端静态资产，必须用应用基址而非 API 基址
    new DictionaryCache(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) },
        builder.Configuration["DictionaryUrl"] ?? "dictionary.json"));
builder.Services.AddScoped<AppState>();

// 界面文案：基础语言随首个里程碑固定 zh-CN（学习配置化在第 4 阶段接入 LearnerProfile）
builder.Services.AddScoped<IUiTextProvider>(_ => new UiTextProvider(
    _.GetRequiredService<IUiTextCatalogQuery>(), baseLanguage: "zh-CN"));

builder.Services.AddAuthorizationCore();

// 启动时预热：主题初始化 + 令牌缓存恢复 + 文案目录加载（失败时保持键回退，不阻断渲染）
var host = builder.Build();
var tokens = host.Services.GetRequiredService<IAuthTokenStore>();
await tokens.RestoreAsync(CancellationToken.None);
var theme = host.Services.GetRequiredService<IThemeService>();
await theme.InitializeAsync(CancellationToken.None);
var uiText = host.Services.GetRequiredService<IUiTextProvider>();
try
{
    await uiText.EnsureLoadedAsync("zh-CN", CancellationToken.None);
}
catch (HttpRequestException)
{
    // 服务端不可用：UI 显示不可用状态（ConnectivityBanner），文案按键回退
}

await host.RunAsync();
