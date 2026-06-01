using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoVerdict.Application.Payments;
using AutoVerdict.Contracts.Configuration;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace AutoVerdict.Infrastructure.Payments;

public sealed class LemonSqueezyPaymentService(
    IOptions<PaymentOptions> opts,
    IHttpClientFactory httpFactory) : IPaymentService
{
    public bool ValidateWebhookSignature(string body, string signature)
    {
        var secret = opts.Value.WebhookSecret;
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(signature))
            return false;

        var computed = Convert.ToHexString(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(secret),
                Encoding.UTF8.GetBytes(body)));

        return string.Equals(computed, signature, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyDictionary<string, PackagePrice>> GetPackagePricesAsync(CancellationToken ct = default)
    {
        var options = opts.Value;
        using var client = httpFactory.CreateClient("lemonsqueezy");

        // Currency is a store-level property, not per-variant.
        var storeResponse = await client.GetAsync($"stores/{options.StoreId}", ct);
        storeResponse.EnsureSuccessStatusCode();
        var storeDoc = JsonDocument.Parse(await storeResponse.Content.ReadAsStringAsync(ct));
        var currency = storeDoc.RootElement
            .GetProperty("data")
            .GetProperty("attributes")
            .GetProperty("currency")
            .GetString() ?? "EUR";

        var result = new Dictionary<string, PackagePrice>();
        foreach (var (packageKey, variantId) in options.PackageVariantIds)
        {
            var response = await client.GetAsync($"variants/{variantId}", ct);
            response.EnsureSuccessStatusCode();

            var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var attrs = doc.RootElement.GetProperty("data").GetProperty("attributes");
            var amountCents = attrs.GetProperty("price").GetInt32();

            result[packageKey] = new PackagePrice(amountCents, currency.ToUpperInvariant());
        }

        return result;
    }

    public async Task<string> CreateCheckoutAsync(
        Guid userId,
        string email,
        string packageKey,
        string successUrl,
        string cancelUrl,
        CancellationToken ct = default)
    {
        var options = opts.Value;

        if (!options.PackageVariantIds.TryGetValue(packageKey, out var variantId))
            throw new InvalidOperationException($"No variant ID configured for package '{packageKey}'.");

        var payload = new
        {
            data = new
            {
                type = "checkouts",
                attributes = new
                {
                    checkout_data = new
                    {
                        custom = new Dictionary<string, string>
                        {
                            ["user_id"] = userId.ToString(),
                            ["package"] = packageKey,
                        },
                        email,
                    },
                    product_options = new { redirect_url = successUrl },
                },
                relationships = new
                {
                    store = new { data = new { type = "stores", id = options.StoreId } },
                    variant = new { data = new { type = "variants", id = variantId } },
                },
            },
        };

        using var client = httpFactory.CreateClient("lemonsqueezy");
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/vnd.api+json");
        var response = await client.PostAsync("checkouts", content, ct);
        response.EnsureSuccessStatusCode();

        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return doc.RootElement
            .GetProperty("data")
            .GetProperty("attributes")
            .GetProperty("url")
            .GetString()
            ?? throw new InvalidOperationException("Lemon Squeezy did not return a checkout URL.");
    }
}
