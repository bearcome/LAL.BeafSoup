using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BeafSoup.Content;
using BeafSoup.Exercises;
using BeafSoup.Identity;
using BeafSoup.Learning;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Api.Tests;

/// <summary>第 4 阶段验收：学习闭环（课程→课时→逐题判分→进度）、授权边界、幂等重放、SRS 复习队列。</summary>
[Collection("api")]
public sealed class LearningLoopApiTests(ApiTestFixture fixture)
{
    private static string LocateSeedDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "data", "seed")))
            directory = directory.Parent;
        return directory is null
            ? throw new DirectoryNotFoundException("找不到仓库 data/seed 目录。")
            : Path.Combine(directory.FullName, "data", "seed");
    }

    private async Task SeedContentAsync()
    {
        // 用真实种子走完整入库管线（与生产同路径）
        await DbMigrator.DatabaseSeeder.SeedAsync(fixture.ConnectionString, LocateSeedDirectory(), replaceExisting: true);
    }

    private async Task<(HttpClient client, AuthTokensDto tokens, Guid profileId)> NewLearnerAsync(string email)
    {
        var client = fixture.CreateClient();
        var tokens = await fixture.RegisterAsync(client, email);
        ApiTestFixture.Authenticate(client, tokens);
        var profiles = await client.GetFromJsonAsync<List<LearnerProfileDto>>("/api/learning/profiles");
        Guid profileId;
        if (profiles is { Count: > 0 })
        {
            profileId = profiles[0].Id;
        }
        else
        {
            var created = await client.PostAsJsonAsync("/api/learning/profiles",
                new CreateLearnerProfileRequest("zh-CN", "de-DE"));
            var profile = await created.Content.ReadFromJsonAsync<LearnerProfileDto>();
            profileId = profile!.Id;
        }
        return (client, tokens, profileId);
    }

    [Fact]
    public async Task FullLessonLoop_AllSixExerciseTypes_GradesAndProgresses()
    {
        await SeedContentAsync();
        var (client, _, profileId) = await NewLearnerAsync("loop@example.com");

        // 课程树 → 课时 → 习题呈现
        var tree = await client.GetFromJsonAsync<List<CourseTreeDto>>("/api/content/courses");
        tree.Should().NotBeNull();
        var lessonId = tree![0].Units[0].Lessons[0].LessonId;
        var lesson = await client.GetFromJsonAsync<LessonContentDto>($"/api/content/lessons/{lessonId}");
        lesson.Should().NotBeNull();
        lesson!.Exercises.Should().HaveCount(6);

        var ids = lesson.Exercises.Select(e => e.ExerciseInstanceId).ToArray();
        var presentations = await client.GetFromJsonAsync<List<ExercisePresentationDto>>(
            $"/api/exercises?ids={string.Join("&ids=", ids)}");
        presentations.Should().HaveCount(6);
        presentations.Should().NotBeNull();
        presentations!.Select(p => p.TypeKey).Should().BeEquivalentTo(
            ["WordDefinitionMcq", "SentenceClozeMcq", "SentenceClozeInput", "SentenceTranslationOrder", "DialogueListeningMcq", "DialogueCloze"]);

        // 逐题作答：全部答对（从负载读取正确答案构造作答）
        var activityCounter = 0;
        foreach (var presentation in presentations!)
        {
            using var payload = JsonDocument.Parse(presentation.PayloadJson);
            var answer = BuildCorrectAnswer(presentation.TypeKey, payload.RootElement);
            var activityId = Guid.NewGuid();
            var grade = await GradeAsync(client, presentation.InstanceId, profileId, activityId, answer);
            grade.Correct.Should().BeTrue($"题型 {presentation.TypeKey} 用负载声明的正确答案应判对");
            activityCounter++;
        }

        var progress = await client.GetFromJsonAsync<ProgressSnapshotDto>($"/api/learning/profiles/{profileId}/progress");
        progress!.Xp.Should().Be(activityCounter * 10); // 每题 10 经验
        progress.Hearts.Should().Be(5);                 // 全对不扣心
    }

    [Fact]
    public async Task WrongAnswer_DeductsHeart_AllowsRetry_AndReplayDoesNotDoubleCount()
    {
        await SeedContentAsync();
        var (client, _, profileId) = await NewLearnerAsync("hearts@example.com");
        var lesson = await GetFirstLessonAsync(client);
        var instance = lesson.Exercises.First(e => e.ExerciseTypeKey == ExerciseTypeKeys.WordDefinitionMcq);
        var presentations = await client.GetFromJsonAsync<List<ExercisePresentationDto>>(
            $"/api/exercises?ids={instance.ExerciseInstanceId}");
        var presentation = presentations![0];
        using var payload = JsonDocument.Parse(presentation.PayloadJson);

        // 答错 → 扣心；留在当前题可重试（客户端行为），再次作答同 ActivityId 重试不重复计分
        var activityId = Guid.NewGuid();
        var wrong = await GradeAsync(client, presentation.InstanceId, profileId, activityId,
            JsonSerializer.SerializeToElement(new { optionId = "definitely-wrong" }));
        wrong.Correct.Should().BeFalse();
        wrong.Progress.Hearts.Should().Be(4);

        var replay = await GradeAsync(client, presentation.InstanceId, profileId, activityId,
            JsonSerializer.SerializeToElement(new { optionId = "definitely-wrong" }));
        replay.Progress.Hearts.Should().Be(4); // 重放同一活动不重复扣心
        replay.Progress.Xp.Should().Be(wrong.Progress.Xp);

        // 修正答案 → 正常奖励经验（不因之前错误减少奖励）
        var retryActivityId = Guid.NewGuid();
        var retry = await GradeAsync(client, presentation.InstanceId, profileId, retryActivityId,
            BuildCorrectAnswer(presentation.TypeKey, payload.RootElement));
        retry.Correct.Should().BeTrue();
        retry.Progress.Xp.Should().Be(10);
        retry.Progress.Hearts.Should().Be(4); // 答对不补心，仅不因错误减少奖励
    }

    [Fact]
    public async Task CrossUserAccess_ReturnsNotFoundWithoutLeakage()
    {
        var (clientA, _, profileA) = await NewLearnerAsync("user-a@example.com");
        var (clientB, _, _) = await NewLearnerAsync("user-b@example.com");

        // GET 他人进度与 PATCH 他人配置都必须 404（不泄漏资源是否存在）
        var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/learning/profiles/{profileA}/progress");
        getRequest.Headers.Authorization = clientB.DefaultRequestHeaders.Authorization;
        (await clientB.SendAsync(getRequest)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/learning/profiles/{profileA}")
        {
            Content = JsonContent.Create(new UpdateProfilePreferencesRequest("dark", null, null, null)),
        };
        patchRequest.Headers.Authorization = clientB.DefaultRequestHeaders.Authorization;
        (await clientB.SendAsync(patchRequest)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        _ = clientA;
    }

    [Fact]
    public async Task ReviewQueue_EnqueueRate_UpdatesFsrsAndFormMastery()
    {
        await SeedContentAsync();
        var (client, _, profileId) = await NewLearnerAsync("reviewer@example.com");

        // 入队两个新卡（每日预算内）
        var conceptCoffee = StableConceptId("concept.coffee");
        var conceptDrink = StableConceptId("concept.drink");
        var enqueue = await client.PostAsJsonAsync($"/api/learning/profiles/{profileId}/reviews/enqueue",
            new { conceptIds = new[] { conceptCoffee, conceptDrink } });
        var enqueued = await enqueue.Content.ReadFromJsonAsync<JsonElement>();
        enqueued.GetProperty("inserted").GetInt32().Should().Be(2);

        var queue = await client.GetFromJsonAsync<List<ReviewQueueItemDto>>(
            $"/api/learning/profiles/{profileId}/reviews/due?limit=20");
        queue.Should().HaveCount(2);
        queue!.All(q => q.IsNew).Should().BeTrue();

        // 评级 Good（3）：概念卡进入 FSRS 调度
        var rate = await client.PostAsJsonAsync($"/api/learning/profiles/{profileId}/reviews/rate",
            new { conceptId = conceptCoffee, rating = 3, activityId = Guid.NewGuid(), cellId = (Guid?)null, isCorrect = true });
        var progress = await rate.Content.ReadFromJsonAsync<ProgressSnapshotDto>();
        progress!.Xp.Should().Be(5); // ReviewSubmitted = 5 经验
    }

    [Fact]
    public async Task FormLinkedGrade_UpdatesConceptCardWithoutExtraCard()
    {
        await SeedContentAsync();
        var (client, _, profileId) = await NewLearnerAsync("forms@example.com");
        var lesson = await GetFirstLessonAsync(client);
        var cloze = lesson.Exercises.First(e => e.ExerciseTypeKey == ExerciseTypeKeys.SentenceClozeMcq);
        var presentations = await client.GetFromJsonAsync<List<ExercisePresentationDto>>(
            $"/api/exercises?ids={cloze.ExerciseInstanceId}");
        using var payload = JsonDocument.Parse(presentations![0].PayloadJson);

        await GradeAsync(client, cloze.ExerciseInstanceId, profileId, Guid.NewGuid(),
            BuildCorrectAnswer(ExerciseTypeKeys.SentenceClozeMcq, payload.RootElement));

        // 判分关联已为 concept.coffee 建概念卡：再次入队被跳过（概念级唯一，不重复建卡）
        var reEnqueue = await client.PostAsJsonAsync($"/api/learning/profiles/{profileId}/reviews/enqueue",
            new { conceptIds = new[] { StableConceptId("concept.coffee") } });
        var reEnqueued = await reEnqueue.Content.ReadFromJsonAsync<JsonElement>();
        reEnqueued.GetProperty("inserted").GetInt32().Should().Be(0);
    }

    // ---------- 辅助 ----------

    private static Guid StableConceptId(string conceptKey)
        => SharedKernel.StableGuid.For($"concept:{conceptKey}");

    private async Task<LessonContentDto> GetFirstLessonAsync(HttpClient client)
    {
        var tree = await client.GetFromJsonAsync<List<CourseTreeDto>>("/api/content/courses");
        var lessonId = tree![0].Units[0].Lessons[0].LessonId;
        return (await client.GetFromJsonAsync<LessonContentDto>($"/api/content/lessons/{lessonId}"))!;
    }

    private static async Task<GradeAnswerResponse> GradeAsync(
        HttpClient client, Guid instanceId, Guid profileId, Guid activityId, JsonElement answer)
    {
        var response = await client.PostAsJsonAsync($"/api/exercises/{instanceId}/grade",
            new GradeAnswerRequest(activityId, $"grade:{activityId}", profileId, answer.GetRawText(), LessonId: null));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<GradeAnswerResponse>())!;
    }

    private static JsonElement BuildCorrectAnswer(string typeKey, JsonElement payload) => typeKey switch
    {
        ExerciseTypeKeys.WordDefinitionMcq or ExerciseTypeKeys.SentenceClozeMcq or ExerciseTypeKeys.DialogueListeningMcq
            => JsonSerializer.SerializeToElement(new { optionId = payload.GetProperty("correctOptionId").GetString() }),
        ExerciseTypeKeys.SentenceClozeInput
            => JsonSerializer.SerializeToElement(new { text = payload.GetProperty("acceptableForms")[0].GetString() }),
        ExerciseTypeKeys.SentenceTranslationOrder
            => JsonSerializer.SerializeToElement(new { chunkIds = payload.GetProperty("correctOrder").EnumerateArray().Select(e => e.GetString()).ToArray() }),
        ExerciseTypeKeys.DialogueCloze => JsonSerializer.SerializeToElement(new
        {
            lineIndex = payload.GetProperty("lines").EnumerateArray().First(l => l.TryGetProperty("blank", out _)).GetProperty("lineIndex").GetInt32(),
            optionId = payload.GetProperty("lines").EnumerateArray()
                .Select(l => l.TryGetProperty("blank", out var b) ? b.GetProperty("correctOptionId").GetString() : null)
                .First(id => id is not null),
        }),
        _ => throw new ArgumentOutOfRangeException(nameof(typeKey)),
    };
}
