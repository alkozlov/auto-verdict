namespace AutoVerdict.ProcessingService.Configuration;

public sealed class NatsOptions
{
    public const string SectionName = "Nats";
    public string Url { get; set; } = "nats://localhost:4222";
}
