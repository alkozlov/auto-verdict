namespace AutoVerdict.Contracts.Configuration;

public class PaymentOptions
{
    public const string SectionName = "Payment";

    public string Provider { get; set; } = "mock";  // "lemonsqueezy" | "mock"
    public string ApiKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;

    // Keys: "credits_1", "credits_3" → Lemon Squeezy variant ID
    public Dictionary<string, string> PackageVariantIds { get; set; } = new();
}
