using BeafSoup.SharedKernel;

namespace BeafSoup.Content;

/// <summary>对话行：说话人标签仅展示，永不进入 TTS 输入；音频文本来自对应语言版本的句子。</summary>
public sealed record DialogueLine(string SpeakerLabel, string SentenceId, VoiceRole VoiceRole);

public sealed class Dialogue
{
    public string Id { get; }

    private readonly List<DialogueLine> _lines = [];
    public IReadOnlyList<DialogueLine> Lines => _lines;

    public Dialogue(string id) => Id = SlugId.Validate(id, nameof(id));

    public DialogueLine AddLine(string speakerLabel, string sentenceId, VoiceRole voiceRole)
    {
        if (string.IsNullOrWhiteSpace(speakerLabel))
            throw new DomainException(ContentErrorCodes.EmptySpeakerLabel, "说话人标签不能为空（仅展示用途）。");
        var line = new DialogueLine(speakerLabel.Trim(), SlugId.Validate(sentenceId, nameof(sentenceId)), voiceRole);
        _lines.Add(line);
        return line;
    }

    /// <summary>播放脚本 = 各行台词文本按序拼接（补全后的完整台词）；绝不包含说话人标签。</summary>
    public IReadOnlyList<string> BuildPlaybackScript(Func<string, string> sentenceTextResolver)
        => _lines.Select(l => sentenceTextResolver(l.SentenceId)).ToArray();
}
