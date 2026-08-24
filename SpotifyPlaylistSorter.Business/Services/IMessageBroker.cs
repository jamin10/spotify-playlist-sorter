namespace SpotifyPlaylistSorter.Business.Services;

public interface IMessageBroker
{
    /// <summary>
    /// Publishes a pre-serialized message to the queue.
    /// </summary>
    Task PublishAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consumes messages from the queue, invoking <paramref name="handler"/> for each one.
    /// Completes when <paramref name="stoppingToken"/> is cancelled.
    /// </summary>
    Task SubscribeAsync(Func<string, CancellationToken, Task> handler, CancellationToken stoppingToken);
}
