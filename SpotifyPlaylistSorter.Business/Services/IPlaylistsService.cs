using SpotifyPlaylistSorter.Business.Models;

namespace SpotifyPlaylistSorter.Business.Services;

public interface IPlaylistsService
{
    /// <summary>
    /// Get the view model for the Playlists page.
    /// </summary>
    /// <returns>A view model.</returns>
    public Task<PlaylistsViewModel> GetPlaylistsViewModel(int page, int pageSize);

    /// <summary>
    /// Get the view model for the Playlist View page.
    /// </summary>
    /// <returns>A view model.</returns>
    public Task<FullPlaylistViewModel> GetFullPlaylistViewModel(string playlistId, int page, int pageSize);

    public Task<bool> AnalysePlaylist(string id);
}
