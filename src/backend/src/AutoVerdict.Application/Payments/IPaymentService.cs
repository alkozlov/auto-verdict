namespace AutoVerdict.Application.Payments;

public interface IPaymentService
{
    bool ValidateWebhookSignature(string body, string signature);

    Task<string> CreateCheckoutAsync(
        Guid userId,
        string email,
        string packageKey,
        string successUrl,
        string cancelUrl,
        CancellationToken ct = default);
}
