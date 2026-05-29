namespace AutoVerdict.Application.AI;

public sealed record AiTextRequest(
    Guid CheckId,
    string Stage,
    string Model,
    string PromptVersion,
    string SystemPrompt,
    IReadOnlyList<AiMessageContent> Messages,
    int MaxTokens);

public abstract record AiMessageContent;

public sealed record AiTextContent(string Text) : AiMessageContent;

public sealed record AiImageContent(byte[] Bytes, string ContentType) : AiMessageContent;
