namespace AutoVerdict.Contracts.Messages;

public sealed record CarCheckRequestedMessage(
    Guid CheckId,
    Guid UserId,
    string ListingUrl,
    DateTimeOffset RequestedAt);
