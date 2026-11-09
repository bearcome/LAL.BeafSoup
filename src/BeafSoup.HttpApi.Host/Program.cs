using System.Text;
using BeafSoup.Content;
using BeafSoup.Content.EntityFrameworkCore;
using BeafSoup.Exercises;
using BeafSoup.Exercises.EntityFrameworkCore;
using BeafSoup.Identity;
using BeafSoup.Identity.EntityFrameworkCore;
using BeafSoup.Learning;
using BeafSoup.Learning.EntityFrameworkCore;
using BeafSoup.Localization;
using BeafSoup.Localization.EntityFrameworkCore;
using BeafSoup.UsageAnalytics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("缺少连接串配置 ConnectionStrings:Default。");

// ---- 模块服务注册（宿主负责组合，模块不互引基础设施）----
builder.Services.Configure<PresenceOptions>(builder.Configuration.GetSection("Presence"));
builder.Services.Configure<PresenceLeaseOptions>(builder.Configuration.GetSection("Presence"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddDbContext<LocalizationDbContext>(options =>
    options.UseSqlite(connectionString, sqlite => sqlite.MigrationsHistoryTable("__EFMigrationsHistory_Localization")));
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlite(connectionString, sqlite => sqlite.MigrationsHistoryTable("__EFMigrationsHistory_Identity")));
builder.Services.AddDbContext<LearningDbContext>(options =>
    options.UseSqlite(connectionString, sqlite => sqlite.MigrationsHistoryTable("__EFMigrationsHistory_Learning")));
builder.Services.AddDbContext<ExercisesDbContext>(options =>
    options.UseSqlite(connectionString, sqlite => sqlite.MigrationsHistoryTable("__EFMigrationsHistory_Exercises")));
builder.Services.AddDbContext<ContentDbContext>(options =>
    options.UseSqlite(connectionString, sqlite => sqlite.MigrationsHistoryTable("__EFMigrationsHistory_Content")));

builder.Services.AddSingleton(TimeProvider.System);

// Localization 模块
builder.Services.AddScoped<ILocalizationReader, LocalizationReader>();
builder.Services.AddScoped<IUiTextCatalogQuery, UiTextCatalogQuery>();

// Identity 模块（含可替换提供方边界，ADR-006）
builder.Services.AddScoped<IUserAccountRepository, UserAccountRepository>();
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
builder.Services.AddScoped<IUserCredentialRepository, UserCredentialRepository>();
builder.Services.AddScoped<IIdentityProvider, PasswordIdentityProvider>();
builder.Services.AddScoped<IPasswordHashingService, Pbkdf2PasswordHashingService>();
builder.Services.AddScoped<IAccessTokenFactory, JwtAccessTokenFactory>();
builder.Services.AddScoped<ISessionAppService, SessionAppService>();
builder.Services.AddScoped<IIdentityAppService, IdentityAppService>();
builder.Services.AddScoped<IAccountStatisticsQuery, AccountStatisticsQuery>();

// UsageAnalytics 模块
builder.Services.AddScoped<IUsageStatisticsQuery, UsageStatisticsQuery>();
builder.Services.AddScoped<IPresenceHeartbeatHandler, PresenceHeartbeatHandler>();

// Content 模块（课程目录只读）
builder.Services.AddScoped<ICourseCatalogReader, CourseCatalogReader>();
builder.Services.AddScoped<ICourseCatalogQuery, CourseCatalogQuery>();

// Exercises 模块（题型注册表 + 判分）
builder.Services.AddSingleton(new ExerciseRegistry(
[
    new WordDefinitionMcqHandler(), new SentenceClozeMcqHandler(), new SentenceClozeInputHandler(),
    new SentenceTranslationOrderHandler(), new DialogueListeningMcqHandler(), new DialogueClozeHandler(),
]));
builder.Services.AddScoped<IExerciseInstanceRepository, ExerciseInstanceRepository>();
builder.Services.AddScoped<IExercisePresentationQuery, ExercisePresentationQuery>();
builder.Services.AddScoped<IExerciseGradingAppService, ExerciseGradingAppService>();

// Learning 模块（学习配置/活动/复习）
builder.Services.Configure<LearningGamificationOptions>(builder.Configuration.GetSection(LearningGamificationOptions.SectionName));
builder.Services.AddSingleton<IFsrsParameterProvider, DefaultFsrsParameterProvider>();
builder.Services.AddScoped<ILearningProfileRepository, LearningProfileRepository>();
builder.Services.AddScoped<IReviewCardRepository, ReviewCardRepository>();
builder.Services.AddScoped<IFormMasteryRepository, FormMasteryRepository>();
builder.Services.AddScoped<ILearningActivityRepository, LearningActivityRepository>();
builder.Services.AddScoped<ILearnerProgressStateRepository, LearnerProgressStateRepository>();
builder.Services.AddScoped<ILearningProfileAppService, LearningProfileAppService>();
builder.Services.AddScoped<ILearningActivityAppService, LearningActivityAppService>();
builder.Services.AddScoped<IReviewAppService, ReviewAppService>();
builder.Services.AddScoped<ILearningProgressQuery, LearningProgressQuery>();
builder.Services.AddScoped<IConceptReviewRecorder, ConceptReviewRecorder>();

// ---- 认证（JWT + 会话活性校验：sid 失效即拒绝，支持单会话顶替）----
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("缺少 JWT 配置。");
if (string.IsNullOrEmpty(jwtOptions.SigningKey))
    throw new InvalidOperationException("缺少 JWT 签名密钥（配置 Jwt:SigningKey，仅存在于服务端机密配置）。");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var sid = context.Principal?.FindFirst("sid")?.Value;
                if (sid is null || !Guid.TryParse(sid, out var sessionId))
                {
                    context.Fail("令牌缺少会话标识。");
                    return;
                }
                var sessions = context.HttpContext.RequestServices.GetRequiredService<ISessionAppService>();
                if (!await sessions.IsSessionActiveAsync(sessionId, context.HttpContext.RequestAborted))
                    context.Fail("会话已失效。");
            },
        };
    });

// ---- 授权：默认全部端点要求认证；统计权限默认拒绝所有人（账户规范）----
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(UsageAnalyticsPermissions.StatisticsPolicy, policy =>
        policy.RequireClaim("permission", UsageAnalyticsPermissions.StatisticsPolicy))
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// ---- CORS：独立静态客户端跨源调用（来源由部署配置）----
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// 领域异常 → 稳定的错误码响应；凭据错误 → 401
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (BeafSoup.SharedKernel.DomainException exception)
    {
        context.Response.StatusCode = exception.Code switch
        {
            IdentityErrorCodes.InvalidCredentials or IdentityErrorCodes.AccountDisabled => StatusCodes.Status401Unauthorized,
            IdentityErrorCodes.DuplicateIdentityLink => StatusCodes.Status409Conflict,
            IdentityErrorCodes.SessionNotFound or IdentityErrorCodes.SessionEnded => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest,
        };
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { errorCode = exception.Code, message = exception.Message });
    }
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.MapIdentityEndpoints();
app.MapUsageAnalyticsEndpoints();
app.MapLocalizationEndpoints();
app.MapContentEndpoints();
app.MapExerciseEndpoints();
app.MapLearningEndpoints();

app.Run();

// 集成测试经由 WebApplicationFactory 引用程序集入口
public partial class Program;
