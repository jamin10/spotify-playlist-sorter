namespace SpotifyPlaylistSorterWeb.Models;

public class PlaylistsViewModel : SpotifyBaseViewModel
{
    public int TotalPlaylists { get; set; }

    public List<PlaylistModel>? Playlists { get; set; }

    public string? NextPage { get; set; }

    public string? PrevPage { get; set; }
}