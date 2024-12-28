using Azure;
using SpotifyPlaylistSorterWeb.Models;

namespace SpotifyPlaylistSorterWeb.Services.Interfaces;

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
    public Task<FullPlaylistViewModel> GetFullPlaylistViewModel();
}
