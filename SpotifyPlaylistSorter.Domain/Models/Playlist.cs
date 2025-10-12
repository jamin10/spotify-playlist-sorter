namespace SpotifyPlaylistSorter.Domain.Models
{
    public class Playlist
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public ICollection<Track>? Tracks { get; set; }
    }
}
