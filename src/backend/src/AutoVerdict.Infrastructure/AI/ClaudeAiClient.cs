using Anthropic;
using Anthropic.Models.Messages;
using AutoVerdict.Application.AI;
using Microsoft.Extensions.Options;

namespace AutoVerdict.Infrastructure.AI;

public sealed class ClaudeAiClient : IAiClient
{
    private const string ProviderName = "Claude";

    private readonly AnthropicClient _client;
    private readonly AiRetryPolicy _retryPolicy;

    public ClaudeAiClient(IOptions<ClaudeOptions> options, AiRetryPolicy retryPolicy)
    {
        _client = new AnthropicClient { ApiKey = options.Value.ApiKey };
        _retryPolicy = retryPolicy;
    }

    public async Task<AiTextResponse> CreateTextAsync(
        AiTextRequest request,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var parameters = new MessageCreateParams
        {
            Model = request.Model,
            MaxTokens = request.MaxTokens,
            System = request.SystemPrompt,
            Messages =
            [
                new MessageParam
                {
                    Role = Role.User,
                    Content = BuildContent(request.Messages),
                },
            ],
        };

        Message response = await _retryPolicy.ExecuteAsync(
            ct => _client.Messages.Create(parameters, cancellationToken: ct),
            cancellationToken);

        var text = response.Content
            .Select(b => b.Value)
            .OfType<TextBlock>()
            .Select(t => t.Text)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Claude returned no text content.");

        return new AiTextResponse(
            text,
            ProviderName,
            request.Model,
            response.Usage.InputTokens,
            response.Usage.OutputTokens,
            DateTimeOffset.UtcNow - startedAt);
    }

    private static List<ContentBlockParam> BuildContent(IReadOnlyList<AiMessageContent> messages)
    {
        var content = new List<ContentBlockParam>();
        foreach (var message in messages)
        {
            switch (message)
            {
                case AiTextContent text:
                    content.Add(new TextBlockParam { Text = text.Text });
                    break;
                case AiImageContent image:
                    content.Add(new ImageBlockParam
                    {
                        Source = new Base64ImageSource
                        {
                            Data = Convert.ToBase64String(image.Bytes),
                            MediaType = image.ContentType,
                        },
                    });
                    break;
            }
        }

        return content;
    }
}
