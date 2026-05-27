namespace AutoVerdict.Contracts.Configuration;

public static class NatsSubjects
{
    public const string CarCheckRequested = "autoverdict.checks.requested";
    public const string CarCheckCrawled = "autoverdict.checks.crawled";
    public const string CarCheckCompleted = "autoverdict.checks.completed";
    public const string CarCheckFailed = "autoverdict.checks.failed";

    public static class Streams
    {
        public const string CarChecks = "AUTOVERDICT_CHECKS";
    }

    public static class Consumers
    {
        public const string ProcessingService = "processing-service";
    }
}
