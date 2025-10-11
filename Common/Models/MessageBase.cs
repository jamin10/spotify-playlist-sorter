using SpotifyPlaylistSorter.Common.Models.Enums;

namespace SpotifyPlaylistSorter.Common.Models.QueueMessages;

public class MessageBase
{
    public QueueMessageTypeEnum Type { get; set; } = QueueMessageTypeEnum.Default;
}