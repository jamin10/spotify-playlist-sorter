namespace SpotifyPlaylistSorter.Domain.Models;

public class Playlist
{
    public int Id { get; set; }
    public string SpotifyPlaylistId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public ICollection<Track> Tracks { get; set; } = [];

    public static Playlist Create(
        string spotifyPlaylistId,
        string name,
        string? description,
        string? imageUrl) => new()
    {
        SpotifyPlaylistId = spotifyPlaylistId,
        Name = name,
        Description = description,
        ImageUrl = imageUrl
    };

    public void AddTrack(Track track)
    {
        if (!Tracks.Contains(track))
            Tracks.Add(track);
    }
}
