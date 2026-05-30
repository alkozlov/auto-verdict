using AutoVerdict.Contracts.Messages;

namespace AutoVerdict.ProcessingService.Pipeline;

public interface ICarCheckPipeline
{
    /// <summary>
    /// Runs the analysis for <paramref name="message"/> and returns the storage key
    /// of the saved report markdown.
    /// </summary>
    Task<string> ExecuteAsync(CarCheckRequestedMessage message, CancellationToken cancellationToken = default);
}
