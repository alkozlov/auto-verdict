namespace AutoVerdict.Application.AI;

public sealed record AiTextResponse(
    string Text,
    string Provider,
    string Model,
    long InputTokens,
    long OutputTokens,
    TimeSpan Duration);
