namespace AutoVerdict.Application.Payments;

public record PackagePrice(int AmountCents, string Currency);

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

    Task<IReadOnlyDictionary<string, PackagePrice>> GetPackagePricesAsync(CancellationToken ct = default);
}
