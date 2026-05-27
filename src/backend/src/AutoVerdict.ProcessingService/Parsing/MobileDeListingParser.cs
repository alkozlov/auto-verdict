using System.Globalization;
using System.Text.RegularExpressions;
using AutoVerdict.Contracts.Listing;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace AutoVerdict.ProcessingService.Parsing;

public sealed partial class MobileDeListingParser(
    IHostEnvironment environment,
    IOptions<PlaywrightParserOptions> options,
    ILogger<MobileDeListingParser> logger) : ICarListingParser
{
    internal static bool IsSupported(string rawUrl)
    {
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
            return false;

        var host = uri.IdnHost.ToLowerInvariant();
        return uri.Scheme is "http" or "https"
               && (host == "mobile.de" || host.EndsWith(".mobile.de", StringComparison.Ordinal));
    }

    public async Task<ListingParseResult> ParseAsync(
        Guid checkId,
        string listingUrl,
        string screenshotStorageKey,
        CancellationToken cancellationToken = default)
    {
        if (!IsSupported(listingUrl))
            throw new InvalidOperationException("Only mobile.de listing URLs are supported.");

        var parserOptions = options.Value;
        bool headless = parserOptions.Headless ?? !environment.IsDevelopment();

        logger.LogInformation(
            "Starting Playwright for check {CheckId} (mobile.de): headless={Headless}, devtools={Devtools}, slowMoMs={SlowMoMs}, debugPauseMs={DebugPauseMs}.",
            checkId, headless, parserOptions.Devtools, parserOptions.SlowMoMs, parserOptions.DebugPauseMs);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless,
            SlowMo = parserOptions.SlowMoMs > 0 ? parserOptions.SlowMoMs : null,
            Channel = string.IsNullOrWhiteSpace(parserOptions.BrowserChannel) ? null : parserOptions.BrowserChannel,
            ExecutablePath = string.IsNullOrWhiteSpace(parserOptions.BrowserExecutablePath)
                ? null
                : parserOptions.BrowserExecutablePath,
            // Suppress the automation flag that sites detect via navigator.webdriver and DevTools protocol
            Args = parserOptions.Devtools && !headless
                ? ["--disable-blink-features=AutomationControlled", "--auto-open-devtools-for-tabs"]
                : ["--disable-blink-features=AutomationControlled"],
        });

        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            DeviceScaleFactor = 1,
            Locale = "de-DE",
            TimezoneId = "Europe/Berlin",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36",
        });

        // Patch remaining JS automation signals before any page script runs
        await context.AddInitScriptAsync("""
            Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
            if (!window.chrome) window.chrome = { runtime: {} };
            Object.defineProperty(navigator, 'languages', { get: () => ['de-DE', 'de', 'en-US', 'en'] });
            Object.defineProperty(navigator, 'plugins', { get: () => [{ name: 'Chromium PDF Plugin' }] });
            """);

        var page = await context.NewPageAsync();

        logger.LogInformation("Opening mobile.de listing {ListingUrl} for check {CheckId}.", listingUrl, checkId);

        // Use Load (not NetworkIdle) — mobile.de's bot-detection loop keeps the network busy,
        // causing NetworkIdle to time out. Load fires once the document and its resources are ready.
        await page.GotoAsync(listingUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = 60_000,
        });

        // Give any post-load JS (redirects, challenge scripts) a moment to settle
        await page.WaitForTimeoutAsync(2_000);

        // 1. Decline cookie consent if the modal is present
        await TryDeclineCookiesAsync(page);

        // 2. Expand all collapsed sections
        await ClickExpandButtonsAsync(page);

        // 3-8. Extract content
        var title = await ExtractTitleAsync(page);
        var price = await ExtractPriceAsync(page);
        var mainDetails = await ExtractMainDetailsAsync(page);
        var technicalDetails = await ExtractTechnicalDetailsAsync(page);
        var equipment = await ExtractEquipmentAsync(page);
        var description = await ExtractDescriptionAsync(page);

        // Merge into attributes dict; first writer wins on key collision
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in mainDetails) attributes.TryAdd(k, v);
        foreach (var (k, v) in technicalDetails) attributes.TryAdd(k, v);
        if (equipment.Count > 0)
            attributes["Equipment"] = string.Join(", ", equipment);
        if (description is not null)
            attributes["Description"] = description;

        var text = await page.Locator("body").InnerTextAsync(new LocatorInnerTextOptions { Timeout = 10_000 });
        var canonicalUrl = await GetCanonicalUrlAsync(page);
        var htmlLanguage = await GetHtmlLanguageAsync(page);
        var currentUrl = page.Url;
        var detectedBlock = DetectBlockOrCaptcha(await page.TitleAsync(), text);

        await page.EvaluateAsync("window.scrollTo(0, 0)");
        var screenshotBytes = await page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = true,
            Type = ScreenshotType.Png,
        });

        var make = GetAttribute(mainDetails, "Marke");
        var model = GetAttribute(mainDetails, "Modell");
        var year = TryParseYear(GetAttribute(mainDetails, "Erstzulassung")) ?? TryParseYear(text);
        var mileageKm = TryParseMileage(GetAttribute(mainDetails, "Kilometerstand"));

        var snapshot = new CarListingSnapshot(
            listingUrl,
            title,
            make,
            model,
            year,
            mileageKm,
            price,
            SellerName: null,
            ExtractSellerType(text),
            GetAttribute(mainDetails, "Standort"),
            description,
            attributes,
            screenshotStorageKey,
            DateTimeOffset.UtcNow);

        return new ListingParseResult(
            snapshot,
            screenshotBytes,
            "image/png",
            DetectedBlockOrCaptcha: detectedBlock,
            CanonicalUrl: canonicalUrl,
            HtmlLanguage: htmlLanguage,
            CurrentUrl: currentUrl);
    }

    // ── Cookie decline ─────────────────────────────────────────────────────────

    // If #mde-consent-modal-container exists, click the "Ablehnen" (decline) button inside it.
    private static async Task TryDeclineCookiesAsync(IPage page)
    {
        try
        {
            var modal = page.Locator("#mde-consent-modal-container");
            if (await modal.CountAsync() == 0) return;

            var ablehnen = modal
                .Locator("button")
                .Filter(new LocatorFilterOptions { HasText = "Ablehnen" })
                .First;

            await ablehnen.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5_000,
            });
            await ablehnen.ClickAsync(new LocatorClickOptions { Timeout = 5_000 });
        }
        catch (TimeoutException) { }
        catch (PlaywrightException) { }
    }

    // ── Expand collapsed sections ──────────────────────────────────────────────

    private static async Task ClickExpandButtonsAsync(IPage page)
    {
        try
        {
            var buttons = page
                .Locator("button")
                .Filter(new LocatorFilterOptions { HasText = "Mehr anzeigen" });

            var count = await buttons.CountAsync();
            for (var i = 0; i < count; i++)
            {
                try
                {
                    await buttons.Nth(i).ClickAsync(new LocatorClickOptions { Timeout = 3_000 });
                    await page.WaitForTimeoutAsync(300);
                }
                catch (PlaywrightException) { }
            }
        }
        catch (PlaywrightException) { }
    }

    // ── Targeted data extractors ───────────────────────────────────────────────

    // Step 3: aside > h2.dNpqi concatenated with aside > div.GOIOV
    private static async Task<string?> ExtractTitleAsync(IPage page)
    {
        var h2 = page.Locator("aside h2.dNpqi").First;
        if (await h2.CountAsync() == 0) return null;

        var h2Text = (await h2.InnerTextAsync()).Trim();

        var subDiv = page.Locator("aside div.GOIOV").First;
        var subText = await subDiv.CountAsync() > 0
            ? (await subDiv.InnerTextAsync()).Trim()
            : null;

        return string.IsNullOrWhiteSpace(subText) ? h2Text : $"{h2Text} {subText}";
    }

    // Step 4: aside > h2[data-testid="vip-leasing-rate-value"]
    private static async Task<decimal?> ExtractPriceAsync(IPage page)
    {
        var el = page.Locator("aside h2[data-testid='vip-leasing-rate-value']").First;
        if (await el.CountAsync() == 0) return null;

        var text = (await el.InnerTextAsync()).Trim();
        return TryParsePrice(text, out var price) ? price : null;
    }

    // Step 5: section[data-testid="content-section"]
    //   → divs whose data-testid starts with "vip-key-features-list-item-"
    //   → first span (any depth) = key, last div (any depth) = value
    private static async Task<Dictionary<string, string>> ExtractMainDetailsAsync(IPage page)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var section = page.Locator("section[data-testid='content-section']").First;
        if (await section.CountAsync() == 0) return result;

        var items = section.Locator("[data-testid^='vip-key-features-list-item-']");
        var count = await items.CountAsync();

        for (var i = 0; i < count; i++)
        {
            var item = items.Nth(i);

            var keySpan = item.Locator("span").First;
            if (await keySpan.CountAsync() == 0) continue;
            var key = (await keySpan.InnerTextAsync()).Trim();

            var valueDivs = item.Locator("div");
            var divCount = await valueDivs.CountAsync();
            if (divCount == 0) continue;
            var value = (await valueDivs.Nth(divCount - 1).InnerTextAsync()).Trim();

            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                result.TryAdd(key, value);
        }

        return result;
    }

    // Step 6: article[data-testid="damageCondition-item"] > dl → dt/dd pairs
    //   If dd contains child elements, pick the first non-img/svg child for text.
    private static async Task<Dictionary<string, string>> ExtractTechnicalDetailsAsync(IPage page)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var article = page.Locator("article[data-testid='damageCondition-item']").First;
        if (await article.CountAsync() == 0) return result;

        var dl = article.Locator("dl").First;
        if (await dl.CountAsync() == 0) return result;

        var nodes = dl.Locator("dt, dd");
        var count = await nodes.CountAsync();

        string? currentKey = null;
        for (var i = 0; i < count; i++)
        {
            var node = nodes.Nth(i);
            var tagName = await node.EvaluateAsync<string>("el => el.tagName.toLowerCase()");

            if (tagName == "dt")
            {
                currentKey = (await node.InnerTextAsync()).Trim();
                continue;
            }

            if (tagName != "dd" || currentKey is null) continue;

            string value;
            var firstNonMediaChild = node.Locator(":scope > *:not(img):not(svg)").First;
            value = await firstNonMediaChild.CountAsync() > 0
                ? (await firstNonMediaChild.InnerTextAsync()).Trim()
                : (await node.InnerTextAsync()).Trim();

            if (!string.IsNullOrEmpty(value))
                result.TryAdd(currentKey, value);

            currentKey = null;
        }

        return result;
    }

    // Step 7: section[data-testid="vip-features-content-section"]
    //   → inner section[data-testid="vip-features-content-section"]
    //   → div[data-testid="vip-features-content"] → ul > li (svg stripped)
    private static async Task<List<string>> ExtractEquipmentAsync(IPage page)
    {
        var result = new List<string>();

        var outerSection = page.Locator("section[data-testid='vip-features-content-section']").First;
        if (await outerSection.CountAsync() == 0) return result;

        var innerSection = outerSection.Locator("section[data-testid='vip-features-content-section']").First;
        var searchRoot = await innerSection.CountAsync() > 0 ? innerSection : outerSection;

        var contentDiv = searchRoot.Locator("div[data-testid='vip-features-content']").First;
        if (await contentDiv.CountAsync() == 0) return result;

        var ul = contentDiv.Locator("ul").First;
        if (await ul.CountAsync() == 0) return result;

        var items = ul.Locator("li");
        var count = await items.CountAsync();

        for (var i = 0; i < count; i++)
        {
            var text = await items.Nth(i).EvaluateAsync<string>("""
                el => {
                    const clone = el.cloneNode(true);
                    clone.querySelectorAll('svg').forEach(s => s.remove());
                    return clone.textContent?.trim() ?? '';
                }
                """);

            if (!string.IsNullOrWhiteSpace(text))
                result.Add(text.Trim());
        }

        return result;
    }

    // Step 8: article[data-testid="vip-vehicle-description-box"]
    //   → section[data-testid="vip-vehicle-description-content-section"]
    //   → div[data-testid="vip-vehicle-description-text"] (inner text preserves formatting)
    private static async Task<string?> ExtractDescriptionAsync(IPage page)
    {
        var article = page.Locator("article[data-testid='vip-vehicle-description-box']").First;
        if (await article.CountAsync() == 0) return null;

        var section = article.Locator("section[data-testid='vip-vehicle-description-content-section']").First;
        if (await section.CountAsync() == 0) return null;

        var div = section.Locator("div[data-testid='vip-vehicle-description-text']").First;
        if (await div.CountAsync() == 0) return null;

        var text = (await div.InnerTextAsync()).Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    // ── Infrastructure helpers ────────────────────────────────────────────────

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
            "captcha", "human verification", "cloudflare",
            "access denied", "403 forbidden", "kein roboter", "sind sie ein mensch",
            // Imperva Bot Manager block page (mobile.de)
            "zugriff verweigert", "error reference",
        ];

        foreach (var signal in signals)
        {
            if (title.Contains(signal, StringComparison.OrdinalIgnoreCase) ||
                bodyText.Contains(signal, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    // ── Text / attribute helpers ──────────────────────────────────────────────

    private static string? GetAttribute(IReadOnlyDictionary<string, string> attributes, string key) =>
        attributes.TryGetValue(key, out var value) ? value : null;

    private static string? ExtractSellerType(string text)
    {
        if (text.Contains("Privatanbieter", StringComparison.OrdinalIgnoreCase) ||
            (text.Contains("Privat", StringComparison.OrdinalIgnoreCase) &&
             !text.Contains("Händler", StringComparison.OrdinalIgnoreCase)))
            return "Private";
        if (text.Contains("Händler", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Dealer", StringComparison.OrdinalIgnoreCase))
            return "Dealer";
        return null;
    }

    private static bool TryParsePrice(string text, out decimal? price)
    {
        price = null;
        var match = PriceRegex().Match(text);
        if (!match.Success) return false;

        // Normalize German/EU number format: strip thousands separators, replace decimal comma
        var normalized = Regex.Replace(match.Groups[1].Value, @"[.\s](?=\d{3})", "").Replace(',', '.');
        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            price = value;

        return price is not null;
    }

    private static int? TryParseYear(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var match = YearRegex().Match(value);
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

    [GeneratedRegex(@"(\d[\d\s.,]{2,})\s*(€|EUR)*", RegexOptions.IgnoreCase)]
    private static partial Regex PriceRegex();

    [GeneratedRegex(@"\b(20[0-3]\d|19[8-9]\d)\b")]
    private static partial Regex YearRegex();

    [GeneratedRegex(@"(\d[\d\s.]{2,})\s*km", RegexOptions.IgnoreCase)]
    private static partial Regex MileageRegex();
}
