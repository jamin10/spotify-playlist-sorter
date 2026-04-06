namespace SpotifyPlaylistSorterWeb.Models;

public class PlaylistModel
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public string? TracksUrl { get; set; }

    public int TracksTotal { get; set; }
}