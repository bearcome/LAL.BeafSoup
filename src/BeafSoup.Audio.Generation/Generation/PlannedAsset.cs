using BeafSoup.SharedKernel;

namespace BeafSoup.Audio.Generation.Generation;

/// <summary>一个待生成的音频资产。</summary>
public sealed record PlannedAsset(
    string RelativePath,
    string TtsText,
    string VoiceName,
    string AssetFileToken,
    string SourceKind,      // word / sentence / dialogue-line
    string SourceKey,       // 语义键或句子 id
    VoiceRole Role);
