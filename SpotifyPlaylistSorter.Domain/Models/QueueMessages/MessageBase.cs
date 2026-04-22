namespace SpotifyPlaylistSorter.Domain.Models.QueueMessages;

public class MessageBase
{
    public QueueMessageTypeEnum Type { get; set; } = QueueMessageTypeEnum.Default;
}
