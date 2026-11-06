namespace BeafSoup.Audio.Generation.Generation;

public interface IAudioSynthesizer
{
    Task SynthesizeToFileAsync(string ssml, string outputPath, CancellationToken cancellationToken);
}
