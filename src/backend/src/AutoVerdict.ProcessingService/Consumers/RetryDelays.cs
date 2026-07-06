namespace AutoVerdict.ProcessingService.Consumers;

/// <summary>Backoff schedule for transient check-processing failures (per JetStream delivery count).</summary>
public static class RetryDelays
{
    private static readonly TimeSpan[] Schedule =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(4),
        TimeSpan.FromMinutes(16),
        TimeSpan.FromMinutes(30),
    ];

    public static TimeSpan ForDelivery(ulong numDelivered)
    {
        var index = Math.Clamp((int)numDelivered - 1, 0, Schedule.Length - 1);
        return Schedule[index];
    }
}
