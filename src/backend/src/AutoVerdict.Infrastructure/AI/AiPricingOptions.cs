namespace AutoVerdict.Infrastructure.AI;

public sealed class AiPricingOptions
{
    public const string SectionName = "AiPricing";

    public decimal UsdToEurRate { get; set; } = 0.92m;
    public Dictionary<string, AiModelPricing> Models { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public AiModelPricing GetModel(string model)
    {
        if (Models.TryGetValue(model, out var pricing))
            return pricing;

        return new AiModelPricing();
    }
}

public sealed class AiModelPricing
{
    public decimal InputPerMillionTokensUsd { get; set; } = 3.00m;
    public decimal OutputPerMillionTokensUsd { get; set; } = 15.00m;
}
