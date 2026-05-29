using System.Text;
using AutoVerdict.Application.AI;
using AutoVerdict.Contracts.Listing;

namespace AutoVerdict.ProcessingService.Pipeline;

public sealed class EvidenceFormatter
{
    private const int MaxDescriptionChars = 18_000;
    private const int MaxAttributeCount = 80;

    public string BuildEvidenceText(EvidenceBundle evidence)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<user-input>");
        sb.AppendLine(Trim(evidence.UserDescriptionMarkdown, MaxDescriptionChars));
        sb.AppendLine("</user-input>");

        if (!string.IsNullOrWhiteSpace(evidence.ListingUrl))
            sb.AppendLine($"Listing URL: {evidence.ListingUrl}");

        if (evidence.CrawledListing is not null)
        {
            sb.AppendLine();
            sb.AppendLine(BuildCrawledListingText(evidence.CrawledListing));
        }

        if (evidence.UserImages.Count > 0)
            sb.AppendLine($"User attached images: {evidence.UserImages.Count}");

        if (evidence.ListingScreenshot is not null)
            sb.AppendLine("Listing screenshot: available");

        return sb.ToString();
    }

    public string BuildFactsText(ExtractedVehicleFacts facts) =>
        AiJson.Serialize(facts);

    public string BuildRisksText(RiskAnalysisResult risks) =>
        AiJson.Serialize(risks);

    private static string BuildCrawledListingText(CarListingSnapshot listing)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<crawled-listing-data>");
        AppendIfPresent(sb, "Title", listing.Title);
        AppendIfPresent(sb, "Make", listing.Make);
        AppendIfPresent(sb, "Model", listing.Model);
        if (listing.Year is not null) sb.AppendLine($"Year: {listing.Year}");
        if (listing.MileageKm is not null) sb.AppendLine($"MileageKm: {listing.MileageKm}");
        if (listing.Price is not null) sb.AppendLine($"Price: {listing.Price}");
        AppendIfPresent(sb, "SellerName", listing.SellerName);
        AppendIfPresent(sb, "SellerType", listing.SellerType);
        AppendIfPresent(sb, "Location", listing.Location);

        if (listing.Attributes.Count > 0)
        {
            sb.AppendLine("Attributes:");
            foreach (var (key, value) in listing.Attributes.Take(MaxAttributeCount))
                sb.AppendLine($"- {key}: {value}");
        }

        AppendIfPresent(sb, "SellerDescription", listing.Description);
        sb.AppendLine("</crawled-listing-data>");
        return sb.ToString();
    }

    private static void AppendIfPresent(StringBuilder sb, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            sb.AppendLine($"{label}: {Trim(value, 5000)}");
    }

    private static string Trim(string value, int maxChars) =>
        value.Length <= maxChars ? value : value[..maxChars] + "\n[truncated]";
}
