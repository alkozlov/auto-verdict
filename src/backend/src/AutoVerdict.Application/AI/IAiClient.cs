namespace AutoVerdict.Application.AI;

public interface IAiClient
{
    Task<AiTextResponse> CreateTextAsync(
        AiTextRequest request,
        CancellationToken cancellationToken = default);
}
