using AutoVerdict.Application.AI;
using AutoVerdict.Infrastructure.AI;
using Microsoft.Extensions.Options;

namespace AutoVerdict.ProcessingService.Pipeline;

public sealed class FactExtractionStage(
    AiStageRunner runner,
    EvidenceFormatter formatter,
    IOptions<AiPipelineOptions> options)
{
    private const string StageName = "FactExtraction";
    private const string PromptVersion = "fact-extraction.v1";
    private const int MaxUserImagesForFactExtraction = 5;

    private readonly AiPipelineOptions _options = options.Value;

    public async Task<ExtractedVehicleFacts> ExecuteAsync(
        EvidenceBundle evidence,
        AiBudgetTracker budget,
        CancellationToken cancellationToken)
    {
        var stage = _options.GetStage(StageName, "claude-haiku-4-5", 2500);
        var messages = new List<AiMessageContent>
        {
            new AiTextContent(
                $$"""
                Extract vehicle facts from the evidence below.

                Return ONLY valid JSON matching this shape:
                {
                  "make": string | null,
                  "model": string | null,
                  "year": number | null,
                  "mileageKm": number | null,
                  "price": number | null,
                  "currency": string | null,
                  "fuelType": string | null,
                  "transmission": string | null,
                  "engine": string | null,
                  "sellerType": string | null,
                  "location": string | null,
                  "vin": string | null,
                  "vinPresent": boolean,
                  "serviceHistoryMentioned": boolean,
                  "accidentFreeClaimed": boolean,
                  "importedMentioned": boolean,
                  "firstOwnerClaimed": boolean,
                  "rawAttributes": { "key": "value" },
                  "evidenceNotes": [string],
                  "missingFields": [string],
                  "confidence": "low" | "medium" | "high"
                }

                Rules:
                - Do not invent facts.
                - Use null when unknown.
                - Preserve useful raw listing attributes.
                - Inspect attached user screenshots/images for visible VIN, mileage, price, year, make, model, seller, and location.
                - If a VIN is visible in an image, extract it exactly and set vinPresent=true.
                - If crawler status is Failed, BlockedOrCaptcha, UnsupportedUrl, or NotProvided, rely on user text and images instead of the URL.
                - Distinguish known facts from assumptions in evidenceNotes.
                - confidence must reflect how complete and consistent the evidence is.

                Evidence:
                {{formatter.BuildEvidenceText(evidence)}}
                """),
        };

        foreach (var image in evidence.UserImages.Take(MaxUserImagesForFactExtraction))
        {
            messages.Add(new AiImageContent(image.Bytes, image.ContentType));
        }

        if (evidence.CrawledListing is null && evidence.ListingScreenshot is not null)
        {
            messages.Add(new AiImageContent(
                evidence.ListingScreenshot.Bytes,
                evidence.ListingScreenshot.ContentType));
        }

        var response = await runner.RunAsync(
            new AiTextRequest(
                evidence.CheckId,
                StageName,
                stage.Model,
                PromptVersion,
                BuildSystemPrompt(),
                messages,
                stage.MaxTokens),
            budget,
            cancellationToken: cancellationToken);

        return AiJson.DeserializeFromModel<ExtractedVehicleFacts>(response.Text);
    }

    private static string BuildSystemPrompt() =>
        """
        You are AutoVerdict's fact extraction component.
        Extract only vehicle purchase facts from the input.
        You are not writing a user-facing report.
        Return strict JSON only. Do not include markdown fences or commentary.
        If the data is missing or uncertain, use null and explain uncertainty in evidenceNotes.
        """;
}
