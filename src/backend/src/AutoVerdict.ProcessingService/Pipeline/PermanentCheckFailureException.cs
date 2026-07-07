namespace AutoVerdict.ProcessingService.Pipeline;

/// <summary>
/// A business-final pipeline failure: retrying cannot succeed (invalid report
/// after repair, AI budget exhausted). The consumer ACKs instead of NAKing.
/// </summary>
public sealed class PermanentCheckFailureException(string message, Exception? inner = null)
    : Exception(message, inner);
