namespace AutoVerdict.ProcessingService.Pipeline;

public sealed class ProcessingOptions
{
    public const string SectionName = "Processing";

    /// <summary>
    /// When true, the real AI pipeline is bypassed. A fake pipeline waits a random
    /// 5–20 s and returns a static pre-prepared report, so local testing incurs no
    /// Claude API costs.
    /// </summary>
    public bool TestMode { get; init; }
}
