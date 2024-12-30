namespace SpotifyPlaylistSorterWeb.Models;

public class FullPlaylistViewModel : SpotifyBaseViewModel
{
    public PaginatedList<TrackModel> Tracks { get; set; } = new PaginatedList<TrackModel>();
}