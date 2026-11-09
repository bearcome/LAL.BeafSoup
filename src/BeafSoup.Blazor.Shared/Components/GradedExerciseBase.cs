using System.Text.Json;
using BeafSoup.Exercises;
using BeafSoup.Blazor.Shared.Services;
using BeafSoup.Learning;
using Microsoft.AspNetCore.Components;

namespace BeafSoup.Blazor.Shared.Components;

/// <summary>已判分题型组件基类：作答 → 服务端权威判分 → 答错本地反馈可重试，答对上报父级自动推进。</summary>
public abstract class GradedExerciseBase : ComponentBase
{
    [Inject] protected ExerciseApiClient Grading { get; set; } = default!;
    [Inject] protected AppState AppState { get; set; } = default!;

    /// <summary>习题呈现（负载已经服务端校验）。</summary>
    [Parameter, EditorRequired] public ExercisePresentationDto Presentation { get; set; } = default!;

    /// <summary>所属课时 ID（可选；判分结果按课时可追溯）。</summary>
    [Parameter] public string? LessonId { get; set; }

    /// <summary>答对后回调（父级自动进入下一题）。</summary>
    [Parameter] public EventCallback<GradeAnswerResponse> OnSolved { get; set; }

    protected GradeAnswerResponse? LastGrade { get; private set; }

    /// <summary>清除判分反馈（学习者修改答案后重试）。</summary>
    protected void ClearFeedback() => LastGrade = null;
    protected bool GradingInProgress { get; private set; }

    protected JsonElement Payload { get; private set; }

    protected override void OnParametersSet()
    {
        using var document = JsonDocument.Parse(Presentation.PayloadJson);
        Payload = document.RootElement.Clone();
    }

    /// <summary>提交作答：每次尝试一个稳定 ActivityId（答错与重试各自记为学习事实，由服务端幂等）。</summary>
    protected async Task SubmitAsync(JsonElement answer)
    {
        if (AppState.CurrentProfile is null || GradingInProgress)
            return;
        GradingInProgress = true;
        try
        {
            var activityId = ExerciseApiClient.NewActivityId();
            var response = await Grading.GradeAsync(Presentation.InstanceId, new GradeAnswerRequest(
                activityId, $"lesson-answer:{activityId}", AppState.CurrentProfile.Id,
                answer.GetRawText(), LessonId is null ? null : null), CancellationToken.None);
            LastGrade = response;
            await AppState.RefreshProgressAsync(CancellationToken.None);
            if (response.Correct)
                await OnSolved.InvokeAsync(response);
        }
        finally
        {
            GradingInProgress = false;
        }
    }
}
