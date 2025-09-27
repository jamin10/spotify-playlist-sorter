namespace SpotifyPlaylistSorter.Business.Services;

public interface IMessageQueueService
{
    /// <summary>
    /// Sends a message to the queue for processing.
    /// </summary>
    /// <param name="message">The message to send.</param>
    Task SendMessage(string message);
}