using System.Text;
using Anthropic;
using Anthropic.Models.Messages;
using AutoVerdict.Application.AI;
using AutoVerdict.Contracts.Listing;
using Microsoft.Extensions.Options;

namespace AutoVerdict.Infrastructure.AI;

public sealed class ClaudeAiAnalysisProvider : IAiAnalysisProvider
{
    private const string ProviderName = "Claude";

    private readonly AnthropicClient _client;
    private readonly ClaudeOptions _options;

    public ClaudeAiAnalysisProvider(IOptions<ClaudeOptions> options)
    {
        _options = options.Value;
        _client = new AnthropicClient { ApiKey = _options.ApiKey };
    }

    public async Task<AiAnalysisResult> AnalyzeAsync(
        AiAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        var parameters = new MessageCreateParams
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            System = BuildSystemPrompt(),
            Messages =
            [
                new()
                {
                    Role = Role.User,
                    Content = BuildUserContent(request),
                },
            ],
        };

        Message response = await _client.Messages.Create(parameters, cancellationToken: cancellationToken);

        string markdownText = response.Content
            .Select(b => b.Value)
            .OfType<TextBlock>()
            .Select(t => t.Text)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Claude returned no text content.");

        return new AiAnalysisResult(
            markdownText,
            ProviderName,
            _options.Model,
            response.Usage.InputTokens,
            response.Usage.OutputTokens);
    }

    private static string BuildSystemPrompt() =>
        """
        You are a professional car search specialist with expertise in European, US, and Asian automotive markets. Your task is to analyze information about a used car and advise a buyer in Poland.

        You will receive a description of the vehicle (which may include copied listing text, seller messages, specs, photos, or inspection notes), an optional listing URL, optionally a screenshot of the listing page, optionally crawled structured data from the listing, and optionally user-provided images.

        Your analysis must be thorough, conservative, and practical. Distinguish model/technical risks from transaction, seller, and deal risks.

        **Output format**: Return your analysis as a Markdown document with exactly the following 9 sections in this order, using the exact headings shown:

        # Car Summary
        One paragraph summarizing the car based on the available information.

        # Listing Facts
        Bullet list of key facts extracted from the provided data:
        - **Make / Model / Year**: ...
        - **Mileage**: ... km
        - **Price**: ... (include currency)
        - **Seller type**: Private / Dealer / Unknown
        - **Location**: ...
        - **Listing URL**: ... (if provided; omit this bullet if not)

        # Model Risks
        Bullet list of known model-specific risks, common faults, or technical issues for this type of vehicle. If there is insufficient information to identify the model, state that clearly.

        # Listing Risks
        Bullet list of risks, inconsistencies, or red flags found in the listing text or images (e.g. mismatched VIN, suspicious mileage, vague description, signs of hidden damage).

        # Deal Risks
        Bullet list of financial, legal, or transactional risks (e.g. price vs. market value, registration complexity, import duties if applicable).

        # Estimated Costs
        Table of estimated one-time and first-year costs for a private buyer in Poland:

        | Item | Estimated Cost |
        |------|---------------|
        | Purchase price | X PLN |
        | Registration fee | X PLN |
        | First-year insurance (OC + AC) | X PLN |
        | Potential immediate repairs | X PLN |
        | **Total** | **X PLN** |

        *Notes: Brief explanation of assumptions and any currency conversions.*

        # Questions for the Seller
        Numbered list of the most important questions to ask the seller before committing to a purchase.

        # Inspection Checklist
        Checklist of items to verify during a physical inspection, using GitHub-flavoured Markdown task syntax:
        - [ ] Item 1
        - [ ] Item 2

        # Recommendation
        A clear, direct verdict: **buy** / **buy with caution** / **avoid**. One paragraph explaining the reasoning.

        ---
        *Disclaimer: This analysis is based solely on the information provided and does not substitute for a professional technical inspection. Always verify the vehicle's documents and consider an independent pre-purchase inspection.*

        **IMPORTANT RULES**:
        - Always include all 9 sections, in the exact order shown, with the exact headings.
        - Use "Unknown" or "Insufficient information" where data is missing — do not speculate.
        - Be concise. Use language understandable to someone without automotive expertise.
        - Focus exclusively on the car, its purchase, and directly related aspects. Ignore anything unrelated.
        - All monetary estimates must be in PLN. If the listing price is in another currency, convert it and state the assumed exchange rate.
        """;

    private static List<ContentBlockParam> BuildUserContent(AiAnalysisRequest request)
    {
        var content = new List<ContentBlockParam>();

        // Listing screenshot first — visual context before user images
        if (request.ListingScreenshotBytes is { Length: > 0 })
        {
            content.Add(new ImageBlockParam
            {
                Source = new Base64ImageSource
                {
                    Data = Convert.ToBase64String(request.ListingScreenshotBytes),
                    MediaType = request.ListingScreenshotContentType,
                },
            });
        }

        // User-provided images
        if (request.UserImages is { Count: > 0 })
        {
            foreach (var image in request.UserImages)
            {
                content.Add(new ImageBlockParam
                {
                    Source = new Base64ImageSource
                    {
                        Data = Convert.ToBase64String(image.Bytes),
                        MediaType = image.ContentType,
                    },
                });
            }
        }

        var urlSection = request.ListingUrl is not null
            ? $"\n\nListing URL: {request.ListingUrl}"
            : string.Empty;

        var crawledDataSection = request.CrawledListing is not null
            ? $"\n\n{BuildCrawledListingText(request.CrawledListing)}"
            : string.Empty;

        content.Add(new TextBlockParam
        {
            Text =
                $"""
                Please analyze this car for a buyer in Poland based on the information provided below.{urlSection}{crawledDataSection}

                <description>
                {request.Description}
                </description>
                """,
        });

        return content;
    }

    private static string BuildCrawledListingText(CarListingSnapshot listing)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<crawled-listing-data>");
        if (listing.Title is not null) sb.AppendLine($"Title: {listing.Title}");
        if (listing.Make is not null) sb.AppendLine($"Make: {listing.Make}");
        if (listing.Model is not null) sb.AppendLine($"Model: {listing.Model}");
        if (listing.Year is not null) sb.AppendLine($"Year: {listing.Year}");
        if (listing.MileageKm is not null) sb.AppendLine($"Mileage: {listing.MileageKm} km");
        if (listing.Price is not null) sb.AppendLine($"Price: {listing.Price}");
        if (listing.SellerName is not null) sb.AppendLine($"Seller: {listing.SellerName}");
        if (listing.SellerType is not null) sb.AppendLine($"Seller type: {listing.SellerType}");
        if (listing.Location is not null) sb.AppendLine($"Location: {listing.Location}");

        var attributes = listing.Attributes
            .Where(kv => !string.Equals(kv.Key, "Description", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (attributes.Count > 0)
        {
            sb.AppendLine("Attributes:");
            foreach (var (key, value) in attributes)
                sb.AppendLine($"  {key}: {value}");
        }

        if (listing.Description is not null)
        {
            sb.AppendLine("Seller description:");
            sb.AppendLine(listing.Description);
        }

        sb.Append("</crawled-listing-data>");
        return sb.ToString();
    }
}
