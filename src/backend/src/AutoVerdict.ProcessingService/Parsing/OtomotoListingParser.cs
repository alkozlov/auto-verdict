using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoVerdict.Contracts.Listing;
using Microsoft.Playwright;

namespace AutoVerdict.ProcessingService.Parsing;

public sealed partial class OtomotoListingParser(ILogger<OtomotoListingParser> logger) : ICarListingParser
{
    public async Task<ListingParseResult> ParseAsync(
        Guid checkId,
        string listingUrl,
        string screenshotStorageKey,
        CancellationToken cancellationToken = default)
    {
        if (!IsOtomotoUrl(listingUrl))
            throw new InvalidOperationException("Only Otomoto.pl listing URLs are supported.");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });

        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            DeviceScaleFactor = 1,
        });

        logger.LogInformation("Opening Otomoto listing {ListingUrl} for check {CheckId}.", listingUrl, checkId);

        await page.GotoAsync(listingUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60_000,
        });

        await TryAcceptCookiesAsync(page);

        var text = await page.Locator("body").InnerTextAsync(new LocatorInnerTextOptions { Timeout = 10_000 });
        var attributes = await ExtractAttributesAsync(page);
        var jsonLd = await ExtractJsonLdAsync(page);

        await page.EvaluateAsync("window.scrollTo(0, 0)");
        var screenshotBytes = await page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = true,
            Type = ScreenshotType.Png,
        });

        var title = FirstNonEmpty(
            GetString(jsonLd, "name"),
            await GetMetaContentAsync(page, "og:title"),
            await page.TitleAsync());
        var description = FirstNonEmpty(
            GetString(jsonLd, "description"),
            await GetMetaContentAsync(page, "og:description"),
            ExtractDescription(text));
        var price = GetDecimal(jsonLd, "price");
        if (price is null && TryParsePrice(text, out var parsedPrice))
            price = parsedPrice;
        var currency = FirstNonEmpty(GetString(jsonLd, "priceCurrency"), ExtractCurrency(text), "PLN");
        var make = FirstNonEmpty(GetNestedString(jsonLd, "brand", "name"), GetAttribute(attributes, "Marka pojazdu"));
        var model = FirstNonEmpty(GetNestedString(jsonLd, "model", "name"), GetAttribute(attributes, "Model pojazdu"));
        var year = TryParseInt(GetAttribute(attributes, "Rok produkcji")) ?? TryParseYear(text);
        var mileageKm = TryParseMileage(GetAttribute(attributes, "Przebieg")) ?? TryParseMileage(text);

        var snapshot = new CarListingSnapshot(
            listingUrl,
            title,
            make,
            model,
            year,
            mileageKm,
            price,
            currency,
            GetAttribute(attributes, "Sprzedający"),
            ExtractSellerType(text),
            GetAttribute(attributes, "Lokalizacja"),
            description,
            attributes,
            screenshotStorageKey,
            DateTimeOffset.UtcNow);

        return new ListingParseResult(snapshot, screenshotBytes, "image/png");
    }

    private static bool IsOtomotoUrl(string rawUrl)
    {
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
            return false;

        var host = uri.IdnHost.ToLowerInvariant();
        return uri.Scheme is "http" or "https"
            && (host == "otomoto.pl" || host.EndsWith(".otomoto.pl", StringComparison.Ordinal));
    }

    private static async Task TryAcceptCookiesAsync(IPage page)
    {
        foreach (var text in new[] { "Akceptuję", "Akceptuje", "Accept", "Zgadzam" })
        {
            try
            {
                var button = page.GetByText(text, new PageGetByTextOptions { Exact = false }).First;
                await button.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 1_000,
                });
                if (await button.IsVisibleAsync())
                {
                    await button.ClickAsync(new LocatorClickOptions { Timeout = 2_000 });
                    return;
                }
            }
            catch (PlaywrightException)
            {
            }
        }
    }

    private static async Task<Dictionary<string, string>> ExtractAttributesAsync(IPage page)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var items = await page.Locator("dl, li, p, div").EvaluateAllAsync<string[]>(
            """
            nodes => nodes
              .map(n => (n.innerText || '').trim())
              .filter(t => t.includes('\n') && t.length < 200)
              .slice(0, 500)
            """);

        foreach (var item in items)
        {
            var parts = item.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                continue;

            var key = parts[0];
            var value = parts[^1];
            if (key.Length is > 1 and < 80 && value.Length is > 0 and < 160)
                result.TryAdd(key, value);
        }

        return result;
    }

    private static async Task<Dictionary<string, JsonElement>> ExtractJsonLdAsync(IPage page)
    {
        var scripts = await page.Locator("script[type='application/ld+json']").EvaluateAllAsync<string[]>(
            "nodes => nodes.map(n => n.textContent || '').filter(Boolean)");

        foreach (var script in scripts)
        {
            try
            {
                using var doc = JsonDocument.Parse(script);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in root.EnumerateArray())
                    {
                        var dict = ToDictionary(item);
                        if (dict.Count > 0)
                            return dict;
                    }
                }
                else
                {
                    var dict = ToDictionary(root);
                    if (dict.Count > 0)
                        return dict;
                }
            }
            catch (JsonException)
            {
            }
        }

        return [];
    }

    private static Dictionary<string, JsonElement> ToDictionary(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return [];

        return element.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<string?> GetMetaContentAsync(IPage page, string property)
    {
        var locator = page.Locator($"meta[property='{property}'], meta[name='{property}']").First;
        return await locator.CountAsync() > 0
            ? await locator.GetAttributeAsync("content")
            : null;
    }

    private static string? GetString(Dictionary<string, JsonElement> json, string key) =>
        json.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? GetNestedString(Dictionary<string, JsonElement> json, string key, string nestedKey)
    {
        if (!json.TryGetValue(key, out var value) || value.ValueKind != JsonValueKind.Object)
            return null;

        return value.TryGetProperty(nestedKey, out var nested) && nested.ValueKind == JsonValueKind.String
            ? nested.GetString()
            : null;
    }

    private static decimal? GetDecimal(Dictionary<string, JsonElement> json, string key)
    {
        if (!json.TryGetValue(key, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
            JsonValueKind.String when decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var number) => number,
            _ => null,
        };
    }

    private static string? GetAttribute(IReadOnlyDictionary<string, string> attributes, string key) =>
        attributes.TryGetValue(key, out var value) ? value : null;

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

    private static string? ExtractDescription(string text)
    {
        var marker = "Opis";
        var index = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;

        var description = text[(index + marker.Length)..].Trim();
        return description.Length > 4000 ? description[..4000] : description;
    }

    private static string? ExtractSellerType(string text)
    {
        if (text.Contains("Osoba prywatna", StringComparison.OrdinalIgnoreCase))
            return "Private";
        if (text.Contains("Firma", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Dealer", StringComparison.OrdinalIgnoreCase))
            return "Dealer";
        return null;
    }

    private static string? ExtractCurrency(string text) =>
        text.Contains("EUR", StringComparison.OrdinalIgnoreCase) ? "EUR" : "PLN";

    private static bool TryParsePrice(string text, out decimal? price)
    {
        price = null;
        var match = PriceRegex().Match(text);
        if (!match.Success)
            return false;

        var normalized = Regex.Replace(match.Groups[1].Value, @"\s+", "");
        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            price = value;

        return price is not null;
    }

    private static int? TryParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = Regex.Replace(value, @"\D", "");
        return int.TryParse(digits, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static int? TryParseYear(string text)
    {
        var match = YearRegex().Match(text);
        return match.Success ? int.Parse(match.Value, CultureInfo.InvariantCulture) : null;
    }

    private static int? TryParseMileage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var match = MileageRegex().Match(value);
        if (!match.Success)
            return null;

        var digits = Regex.Replace(match.Groups[1].Value, @"\D", "");
        return int.TryParse(digits, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    [GeneratedRegex(@"(\d[\d\s]{2,})\s*(zł|PLN|EUR)", RegexOptions.IgnoreCase)]
    private static partial Regex PriceRegex();

    [GeneratedRegex(@"\b(20[0-3]\d|19[8-9]\d)\b")]
    private static partial Regex YearRegex();

    [GeneratedRegex(@"(\d[\d\s]{2,})\s*km", RegexOptions.IgnoreCase)]
    private static partial Regex MileageRegex();
}
