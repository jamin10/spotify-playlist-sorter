namespace SpotifyPlaylistSorterWeb.Models;

public class FullPlaylistModel
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public string? TracksUrl { get; set; }

    public int TracksTotal { get; set; }

    //public PaginatedList<TrackModel>? Tracks { get; set; }
}