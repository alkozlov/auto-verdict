namespace AutoVerdict.Contracts.Messages;

public sealed record CarCheckRequestedMessage(
    Guid CheckId,
    Guid UserId,
    string Description,
    string? ListingUrl,
    DateTimeOffset RequestedAt,
    string ReportLocale = "en",
    string[] UserImageKeys = default!);
