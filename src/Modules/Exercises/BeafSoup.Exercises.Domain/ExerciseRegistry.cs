using BeafSoup.SharedKernel;

namespace BeafSoup.Exercises;

/// <summary>题型注册表：课程引擎只经注册表解析题型；未知键/版本明确失败，不静默退化。</summary>
public sealed class ExerciseRegistry
{
    private readonly IReadOnlyDictionary<string, IExerciseHandler> _handlersByKey;

    public ExerciseRegistry(IEnumerable<IExerciseHandler> handlers)
    {
        var handlersList = handlers.ToList();
        var duplicates = handlersList.GroupBy(h => h.TypeKey).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
        if (duplicates.Length > 0)
            throw new DomainException(ExerciseErrorCodes.UnknownExerciseType,
                $"题型注册键重复：{string.Join(", ", duplicates)}。");
        _handlersByKey = handlersList.ToDictionary(h => h.TypeKey, h => h);
        // 六种首发题型必须全部注册（exercise-types.md）
        var missing = ExerciseTypeKeys.All.Where(key => !_handlersByKey.ContainsKey(key)).ToArray();
        if (missing.Length > 0)
            throw new DomainException(ExerciseErrorCodes.UnknownExerciseType,
                $"题型注册表缺少首发题型：{string.Join(", ", missing)}。");
    }

    public IReadOnlyCollection<string> RegisteredKeys => _handlersByKey.Keys.ToArray();

    public IExerciseHandler Resolve(string typeKey, int payloadVersion)
    {
        if (!_handlersByKey.TryGetValue(typeKey, out var handler))
            throw new DomainException(ExerciseErrorCodes.UnknownExerciseType, $"未注册的题型键 '{typeKey}'。");
        if (handler.PayloadVersion != payloadVersion)
            throw new DomainException(ExerciseErrorCodes.UnsupportedPayloadVersion,
                $"题型 '{typeKey}' 负载版本 {payloadVersion} 不受支持（当前 {handler.PayloadVersion}）。");
        return handler;
    }
}
