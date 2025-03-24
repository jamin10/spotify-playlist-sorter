namespace SpotifyPlaylistSorterWeb.Models;

public class PlaylistsViewModel : SpotifyBaseViewModel
{
    public PlaylistsViewModel(int page, int pageSize)
    {
        Playlists = new PaginatedList<FullPlaylistModel>
        {
            Index = page,
            Limit = pageSize
        };
    }
    public PaginatedList<FullPlaylistModel> Playlists { get; set; }
}