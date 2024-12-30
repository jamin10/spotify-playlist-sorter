namespace SpotifyPlaylistSorterWeb.Models;

public class PlaylistsViewModel : SpotifyBaseViewModel
{
    public PaginatedList<FullPlaylistModel> Playlists { get; set; } = new PaginatedList<FullPlaylistModel>();
}