using BeafSoup.Audio.Generation;
using BeafSoup.Audio.Generation.Generation;
using BeafSoup.Audio.Generation.Tts;

try
{
    var cli = CliOptions.Parse(args);
    var options = ToolOptions.Load(cli.ConfigPath);

    var seed = SeedLoader.Load(options.SeedDirectory);
    var validation = SeedValidator.Validate(seed);
    if (!validation.IsValid)
    {
        foreach (var error in validation.Errors)
            Console.Error.WriteLine($"[种子错误] {error}");
        return 1;
    }
    var validated = validation.Value!;

    if (cli.Command == "validate")
    {
        Console.WriteLine($"种子校验通过：概念 {validated.Concepts.Count}，句子 {validated.Sentences.Count}，对话 {validated.Dialogues.Count}，课程 {validated.Courses.Count}，音色配置 {validated.VoiceRoles.Count}，本地化文本 {validated.LocalizedTexts.Count}。");
        return 0;
    }

    var plan = AudioAssetPlanner.Plan(validated, options);
    if (cli.DryRun)
    {
        foreach (var asset in plan)
            Console.WriteLine($"[dry-run] {asset.RelativePath} <= {asset.SourceKind}:{asset.SourceKey} [{asset.Role}] \"{asset.TtsText}\"");
        Console.WriteLine($"共 {plan.Count} 个资产。");
        return 0;
    }

    var secretsPath = cli.SecretsPath ?? Path.GetFullPath("../../../../../secret.json", AppContext.BaseDirectory);
    var azure = AzureTtsOptions.Load(secretsPath);
    var ssmlBuilder = new SsmlBuilder();
    var cefrByLexeme = validated.Concepts
        .SelectMany(c => c.Lexemes)
        .ToDictionary(l => l.Id.ToString(), l => l.Cefr.ToString());
    var written = new List<string>();
    var synthesizer = new AzureAudioSynthesizer(azure);

    foreach (var asset in plan)
    {
        var outputPath = Path.Combine(options.OutputDirectory, asset.RelativePath);
        if (File.Exists(outputPath) && !cli.Force)
        {
            Console.WriteLine($"[跳过] {asset.RelativePath}（已存在）");
            written.Add(asset.RelativePath);
            continue;
        }
        var rate = asset.SourceKind == "word" && cefrByLexeme.TryGetValue(asset.SourceKey, out var cefr)
            ? options.CefrRates.GetValueOrDefault(cefr)
            : null;
        var pitch = options.RoleProsody.GetValueOrDefault(asset.AssetFileToken)?.Pitch;
        var ssml = ssmlBuilder.Build(asset.VoiceName, asset.TtsText, rate: rate, pitch: pitch);
        await synthesizer.SynthesizeToFileAsync(ssml, outputPath, CancellationToken.None);
        written.Add(asset.RelativePath);
        Console.WriteLine($"[生成] {asset.RelativePath}");
    }

    ManifestWriter.Write(options.OutputDirectory, plan, written);
    Console.WriteLine($"完成：{written.Count} 个资产，manifest 位于 {Path.Combine(options.OutputDirectory, "manifest.json")}。");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[错误] {ex.Message}");
    return 1;
}
