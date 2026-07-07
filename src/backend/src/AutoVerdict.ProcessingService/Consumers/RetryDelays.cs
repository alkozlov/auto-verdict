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
        if (numDelivered <= 1) return Schedule[0];
        if (numDelivered >= (ulong)Schedule.Length) return Schedule[^1];
        return Schedule[(int)numDelivered - 1];
    }
}
