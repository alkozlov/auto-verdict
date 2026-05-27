namespace AutoVerdict.Contracts.Configuration;

public record CreditPackage(string Key, int Credits, int PricePln, string Label)
{
    public static readonly CreditPackage Credits1 = new("credits_1", 1, 20, "1 check");
    public static readonly CreditPackage Credits3 = new("credits_3", 3, 40, "3 checks");

    public static readonly IReadOnlyList<CreditPackage> All = [Credits1, Credits3];

    public static CreditPackage? FindByKey(string key) =>
        All.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
}
