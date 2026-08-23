namespace SpotifyPlaylistSorter.Domain.Models;

public class Album
{
    public int Id { get; set; }
    public string SpotifyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<Artist> Artists { get; set; } = [];
    public ICollection<Track> Tracks { get; set; } = [];

    public static Album Create(string spotifyId, string name, List<Artist> artists) => new()
    {
        SpotifyId = spotifyId,
        Name = name,
        Artists = artists
    };
}
