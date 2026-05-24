using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoVerdict.Contracts.Listing;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace AutoVerdict.ProcessingService.Parsing;

public sealed partial class OtomotoListingParser(
    IHostEnvironment environment,
    IOptions<PlaywrightParserOptions> options,
    ILogger<OtomotoListingParser> logger) : ICarListingParser
{
    public async Task<ListingParseResult> ParseAsync(
        Guid checkId,
        string listingUrl,
        string screenshotStorageKey,
        CancellationToken cancellationToken = default)
    {
        if (!IsOtomotoUrl(listingUrl))
            throw new InvalidOperationException("Only Otomoto.pl listing URLs are supported.");

        var parserOptions = options.Value;
        bool headless = parserOptions.Headless ?? !environment.IsDevelopment();

        logger.LogInformation(
            "Starting Playwright for check {CheckId}: headless={Headless}, devtools={Devtools}, slowMoMs={SlowMoMs}, debugPauseMs={DebugPauseMs}.",
            checkId, headless, parserOptions.Devtools, parserOptions.SlowMoMs, parserOptions.DebugPauseMs);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless,
            SlowMo = parserOptions.SlowMoMs > 0 ? parserOptions.SlowMoMs : null,
            Channel = string.IsNullOrWhiteSpace(parserOptions.BrowserChannel) ? null : parserOptions.BrowserChannel,
            ExecutablePath = string.IsNullOrWhiteSpace(parserOptions.BrowserExecutablePath) ? null : parserOptions.BrowserExecutablePath,
            Args = parserOptions.Devtools && !headless ? ["--auto-open-devtools-for-tabs"] : null,
        });

        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            DeviceScaleFactor = 1,
        });
        var page = await context.NewPageAsync();

        logger.LogInformation("Opening Otomoto listing {ListingUrl} for check {CheckId}.", listingUrl, checkId);

        await page.GotoAsync(listingUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60_000,
        });

        await TryAcceptCookiesAsync(page);

        if (await PauseForDebuggingAsync(parserOptions, cancellationToken))
        {
            logger.LogInformation("Debug pause finished. Reloading listing {ListingUrl} before extraction.", listingUrl);
            await page.GotoAsync(listingUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60_000,
            });
        }

        // Extract structured data using specific selectors per parsing rules
        var titleFromPage    = await ExtractTitleAsync(page);
        var (price, currency) = await ExtractPriceAsync(page);
        var mainDetails      = await ExtractMainDetailsSectionAsync(page);
        var description      = await ExtractDescriptionAsync(page);
        var basicInfo        = await ExtractBasicInformationAsync(page);
        var specyfikacja     = await ExtractAccordionSectionAsync(page, "Specyfikacja");
        var stanIHistoria    = await ExtractAccordionSectionAsync(page, "Stan i historia");

        // Merge all attribute sections; first writer wins on key collision
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in mainDetails)   attributes.TryAdd(k, v);
        foreach (var (k, v) in basicInfo)     attributes.TryAdd(k, v);
        foreach (var (k, v) in specyfikacja)  attributes.TryAdd(k, v);
        foreach (var (k, v) in stanIHistoria) attributes.TryAdd(k, v);
        if (description is not null)
            attributes["Description"] = description;

        // JSON-LD as fallback for title and price
        var jsonLd = await ExtractJsonLdAsync(page);

        var text = await page.Locator("body").InnerTextAsync(new LocatorInnerTextOptions { Timeout = 10_000 });
        var canonicalUrl   = await GetCanonicalUrlAsync(page);
        var htmlLanguage   = await GetHtmlLanguageAsync(page);
        var currentUrl     = page.Url;
        var detectedBlock  = DetectBlockOrCaptcha(await page.TitleAsync(), text);

        await page.EvaluateAsync("window.scrollTo(0, 0)");
        var screenshotBytes = await page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = true,
            Type = ScreenshotType.Png,
        });

        var title = FirstNonEmpty(
            titleFromPage,
            GetString(jsonLd, "name"),
            await GetMetaContentAsync(page, "og:title"),
            await page.TitleAsync());

        price    ??= GetDecimal(jsonLd, "price");
        currency ??= FirstNonEmpty(GetString(jsonLd, "priceCurrency"), "PLN");

        var make      = FirstNonEmpty(GetAttribute(attributes, "Marka pojazdu"), GetNestedString(jsonLd, "brand", "name"));
        var model     = FirstNonEmpty(GetAttribute(attributes, "Model pojazdu"), GetNestedString(jsonLd, "model", "name"));
        var year      = TryParseInt(GetAttribute(attributes, "Rok produkcji")) ?? TryParseYear(text);
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

        return new ListingParseResult(
            snapshot, screenshotBytes, "image/png",
            DetectedBlockOrCaptcha: detectedBlock,
            CanonicalUrl: canonicalUrl,
            HtmlLanguage: htmlLanguage,
            CurrentUrl: currentUrl);
    }

    // ── Cookie acceptance ──────────────────────────────────────────────────────

    private static async Task TryAcceptCookiesAsync(IPage page)
    {
        try
        {
            // Step 1: open cookie settings
            var settingsButton = page.Locator("#onetrust-pc-btn-handler").First;
            await settingsButton.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5_000,
            });
            await settingsButton.ClickAsync(new LocatorClickOptions { Timeout = 5_000 });

            // Step 2: refuse all in the opened dialog
            var refuseButton = page.Locator("#ot-pc-refuse-all-handler").First;
            await refuseButton.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5_000,
            });
            await refuseButton.ClickAsync(new LocatorClickOptions { Timeout = 5_000 });
        }
        catch (TimeoutException) { }
        catch (PlaywrightException) { }
    }

    // ── Targeted data extractors ───────────────────────────────────────────────

    private static async Task<string?> ExtractTitleAsync(IPage page)
    {
        var h1 = page.Locator("h1.offer-title.big.text").First;
        if (await h1.CountAsync() == 0) return null;
        return (await h1.InnerTextAsync()).Trim().NullIfEmpty();
    }

    private static async Task<(decimal? Price, string? Currency)> ExtractPriceAsync(IPage page)
    {
        var h3 = page.Locator("h3.offer-price__number").First;
        if (await h3.CountAsync() == 0) return (null, null);

        var priceText = (await h3.InnerTextAsync()).Trim();
        if (string.IsNullOrWhiteSpace(priceText)) return (null, null);

        if (TryParsePrice(priceText, out var price))
        {
            var currency = priceText.Contains("EUR", StringComparison.OrdinalIgnoreCase) ? "EUR" : "PLN";
            return (price, currency);
        }

        // Fallback: strip non-digits and try plain number
        var digits = Regex.Replace(priceText, @"\D", "");
        if (decimal.TryParse(digits, CultureInfo.InvariantCulture, out var plain))
            return (plain, null);

        return (null, null);
    }

    // data-testid="main-details-section" → pairs of <p> elements inside each child div
    private static async Task<Dictionary<string, string>> ExtractMainDetailsSectionAsync(IPage page)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var section = page.Locator("[data-testid='main-details-section']").First;
        if (await section.CountAsync() == 0) return result;

        var items = section.Locator(":scope > div");
        var count = await items.CountAsync();

        for (var i = 0; i < count; i++)
        {
            var ps = items.Nth(i).Locator("p");
            if (await ps.CountAsync() < 2) continue;

            var key   = (await ps.First.InnerTextAsync()).Trim();
            var value = (await ps.Last.InnerTextAsync()).Trim();
            if (!string.IsNullOrEmpty(key))
                result.TryAdd(key, value);
        }

        return result;
    }

    // data-testid="content-description-section" → second direct div → expand button if present → textWrapper
    private static async Task<string?> ExtractDescriptionAsync(IPage page)
    {
        var section = page.Locator("[data-testid='content-description-section']").First;
        if (await section.CountAsync() == 0) return null;

        var divs = section.Locator(":scope > div");
        if (await divs.CountAsync() < 2) return null;

        var target = divs.Nth(1);

        // Expand description if collapsed
        var expandButton = target.Locator("button").First;
        if (await expandButton.CountAsync() > 0)
        {
            try
            {
                await expandButton.ClickAsync(new LocatorClickOptions { Timeout = 3_000 });
                await page.WaitForTimeoutAsync(300);
            }
            catch (PlaywrightException) { }
        }

        var textWrapper = target.Locator("[data-testid='textWrapper']").First;
        if (await textWrapper.CountAsync() == 0) return null;

        return (await textWrapper.InnerTextAsync()).Trim().NullIfEmpty();
    }

    // data-testid="basic_information" → divs.flex.place-items-center → p.text-foreground-secondary + p.font-normal
    private static async Task<Dictionary<string, string>> ExtractBasicInformationAsync(IPage page)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "VIN", "Kup ten pojazd na raty"
        };

        var section = page.Locator("[data-testid='basic_information']").First;
        if (await section.CountAsync() == 0) return result;

        var items = section.Locator("div.flex.place-items-center");
        var count = await items.CountAsync();

        for (var i = 0; i < count; i++)
        {
            var item  = items.Nth(i);
            var keyEl = item.Locator("p.text-foreground-secondary").First;
            var valEl = item.Locator("p.font-normal").First;

            if (await keyEl.CountAsync() == 0 || await valEl.CountAsync() == 0) continue;

            var key   = (await keyEl.InnerTextAsync()).Trim();
            var value = (await valEl.InnerTextAsync()).Trim();

            if (!string.IsNullOrEmpty(key) && !excluded.Contains(key))
                result.TryAdd(key, value);
        }

        return result;
    }

    // Accordion button (by exact text) → next sibling div → div.flex.place-items-center[data-testid] pairs
    private static async Task<Dictionary<string, string>> ExtractAccordionSectionAsync(IPage page, string buttonText)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var button = page.Locator("button").Filter(new LocatorFilterOptions { HasText = buttonText }).First;
        if (await button.CountAsync() == 0) return result;

        try
        {
            await button.ClickAsync(new LocatorClickOptions { Timeout = 5_000 });
            await page.WaitForTimeoutAsync(500);
        }
        catch (PlaywrightException) { return result; }

        // The content panel is the div immediately following the button in the DOM
        var panel = page.Locator($"xpath=//button[normalize-space(.)='{buttonText}']/following-sibling::div[1]").First;
        if (await panel.CountAsync() == 0) return result;

        var items = panel.Locator("div.flex.place-items-center[data-testid]");
        var count = await items.CountAsync();

        for (var i = 0; i < count; i++)
        {
            var item  = items.Nth(i);
            var keyEl = item.Locator("p.text-foreground-secondary").First;
            var valEl = item.Locator("p.font-normal").First;

            if (await keyEl.CountAsync() == 0 || await valEl.CountAsync() == 0) continue;

            var key   = (await keyEl.InnerTextAsync()).Trim();
            var value = (await valEl.InnerTextAsync()).Trim();

            if (!string.IsNullOrEmpty(key))
                result.TryAdd(key, value);
        }

        return result;
    }

    // ── Infrastructure helpers ────────────────────────────────────────────────

    private static async Task<bool> PauseForDebuggingAsync(PlaywrightParserOptions options, CancellationToken ct)
    {
        if (options.DebugPauseMs <= 0) return false;
        await Task.Delay(options.DebugPauseMs, ct);
        return true;
    }

    private static bool IsOtomotoUrl(string rawUrl)
    {
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
            return false;

        var host = uri.IdnHost.ToLowerInvariant();
        return uri.Scheme is "http" or "https"
            && (host == "otomoto.pl" || host.EndsWith(".otomoto.pl", StringComparison.Ordinal));
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
                        if (dict.Count > 0) return dict;
                    }
                }
                else
                {
                    var dict = ToDictionary(root);
                    if (dict.Count > 0) return dict;
                }
            }
            catch (JsonException) { }
        }

        return [];
    }

    private static async Task<string?> GetCanonicalUrlAsync(IPage page)
    {
        var locator = page.Locator("link[rel='canonical']").First;
        return await locator.CountAsync() > 0
            ? await locator.GetAttributeAsync("href")
            : null;
    }

    private static async Task<string?> GetHtmlLanguageAsync(IPage page)
    {
        var lang = await page.EvaluateAsync<string?>("() => document.documentElement.lang || null");
        return string.IsNullOrWhiteSpace(lang) ? null : lang;
    }

    private static bool DetectBlockOrCaptcha(string title, string bodyText)
    {
        ReadOnlySpan<string> signals =
        [
            "nie jesteś robotem", "nie jestes robotem",
            "captcha", "human verification",
            "cloudflare", "access denied", "403 forbidden"
        ];

        foreach (var signal in signals)
        {
            if (title.Contains(signal, StringComparison.OrdinalIgnoreCase) ||
                bodyText.Contains(signal, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static async Task<string?> GetMetaContentAsync(IPage page, string property)
    {
        var locator = page.Locator($"meta[property='{property}'], meta[name='{property}']").First;
        return await locator.CountAsync() > 0
            ? await locator.GetAttributeAsync("content")
            : null;
    }

    // ── JSON-LD helpers ───────────────────────────────────────────────────────

    private static Dictionary<string, JsonElement> ToDictionary(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return [];
        return element.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase);
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
        if (!json.TryGetValue(key, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var n) => n,
            JsonValueKind.String when decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var n) => n,
            _ => null,
        };
    }

    // ── Text / attribute helpers ──────────────────────────────────────────────

    private static string? GetAttribute(IReadOnlyDictionary<string, string> attributes, string key) =>
        attributes.TryGetValue(key, out var value) ? value : null;

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

    private static string? ExtractSellerType(string text)
    {
        if (text.Contains("Osoba prywatna", StringComparison.OrdinalIgnoreCase))
            return "Private";
        if (text.Contains("Firma", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Dealer", StringComparison.OrdinalIgnoreCase))
            return "Dealer";
        return null;
    }

    private static bool TryParsePrice(string text, out decimal? price)
    {
        price = null;
        var match = PriceRegex().Match(text);
        if (!match.Success) return false;

        var normalized = Regex.Replace(match.Groups[1].Value, @"\s+", "");
        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            price = value;

        return price is not null;
    }

    private static int? TryParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
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
        if (string.IsNullOrWhiteSpace(value)) return null;
        var match = MileageRegex().Match(value);
        if (!match.Success) return null;

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

file static class StringExtensions
{
    internal static string? NullIfEmpty(this string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
