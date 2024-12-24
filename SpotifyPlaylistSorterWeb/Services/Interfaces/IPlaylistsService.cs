using SpotifyPlaylistSorterWeb.Models;

namespace SpotifyPlaylistSorterWeb.Services.Interfaces;

public interface IPlaylistsService
{
    /// <summary>
    /// Get the view model for the Playlists page.
    /// </summary>
    /// <returns>A view model.</returns>
    public Task<PlaylistsViewModel> GetPlaylistsViewModel();
}
