namespace AutoVerdict.Application.AI;

public sealed record AiAnalysisResult(
    string MarkdownText,
    string ProviderName,
    string ModelName,
    long InputTokens,
    long OutputTokens);
