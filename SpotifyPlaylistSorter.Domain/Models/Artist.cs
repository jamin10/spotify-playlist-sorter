namespace SpotifyPlaylistSorter.Domain.Models;

public class Artist
{
    public int Id { get; set; }
    public string SpotifyArtistId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<Track> Tracks { get; set; } = [];
    public ICollection<Album> Albums { get; set; } = [];

    public static Artist Create(string spotifyArtistId, string name) => new()
    {
        SpotifyArtistId = spotifyArtistId,
        Name = name
    };
}
