namespace AutoVerdict.ProcessingService.Parsing;

public sealed class PlaywrightParserOptions
{
    public const string SectionName = "Playwright";

    public bool? Headless { get; set; }
    public bool Devtools { get; set; }
    public int SlowMoMs { get; set; }
    public int DebugPauseMs { get; set; }
    public string? BrowserChannel { get; set; }
    public string? BrowserExecutablePath { get; set; }
}
