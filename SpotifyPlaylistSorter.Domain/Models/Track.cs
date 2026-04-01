namespace SpotifyPlaylistSorter.Domain.Models;

public class Track
{
    public int Id { get; set; }
    public string SpotifyTrackId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int? AlbumId { get; set; }
    public Album? Album { get; set; }
    public ICollection<Artist> Artists { get; set; } = [];
    public ICollection<Playlist> Playlists { get; set; } = [];
    public TrackAudioFeatures? AudioFeatures { get; set; }
}
