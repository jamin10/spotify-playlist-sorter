using SpotifyPlaylistSorterWeb.Models.Enums;

namespace SpotifyPlaylistSorterWeb.Models.QueueMessages;

public class MessageBase
{
    public QueueMessageTypeEnum Type { get; set; } = QueueMessageTypeEnum.Default;
}