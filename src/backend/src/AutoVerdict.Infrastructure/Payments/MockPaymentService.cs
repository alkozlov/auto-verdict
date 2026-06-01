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

    public Task<IReadOnlyDictionary<string, PackagePrice>> GetPackagePricesAsync(CancellationToken ct = default)
    {
        IReadOnlyDictionary<string, PackagePrice> prices = new Dictionary<string, PackagePrice>
        {
            ["credits_1"] = new(499, "EUR"),
            ["credits_3"] = new(999, "EUR"),
        };
        return Task.FromResult(prices);
    }
}
