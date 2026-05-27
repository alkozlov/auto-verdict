using AutoVerdict.Application.Payments;

namespace AutoVerdict.Infrastructure.Payments;

public sealed class MockPaymentService : IPaymentService
{
    public bool ValidateWebhookSignature(string body, string signature) => true;

    public Task<string> CreateCheckoutAsync(
        Guid userId,
        string email,
        string packageKey,
        string successUrl,
        string cancelUrl,
        CancellationToken ct = default)
    {
        var url = $"/api/payments/mock-checkout"
            + $"?userId={userId}"
            + $"&package={Uri.EscapeDataString(packageKey)}"
            + $"&successUrl={Uri.EscapeDataString(successUrl)}";

        return Task.FromResult(url);
    }
}
