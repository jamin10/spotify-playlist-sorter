namespace SpotifyPlaylistSorter.Common.Models.QueueMessages
{
    public class AnalysePlaylist : MessageBase
    {
        public string PlaylistId { get; set; }

        public List<string> TrackIds { get; set; } = new List<string>();

        public AnalysePlaylist(string playlistId, List<string> trackIds)
        {
            Type = Enums.QueueMessageTypeEnum.AnalysePlaylist;
            PlaylistId = playlistId;
            TrackIds = trackIds ?? new List<string>();
        }
    }
}