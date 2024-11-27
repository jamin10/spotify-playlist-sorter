using SpotifyPlaylistSorterWeb.Models;

namespace SpotifyPlaylistSorterWeb.Services.Interfaces;

public interface IPlaylistsService
{
    public Task<PlaylistsViewModel> GetPlaylistsViewModel();
}
