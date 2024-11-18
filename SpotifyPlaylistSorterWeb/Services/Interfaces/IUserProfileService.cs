using SpotifyPlaylistSorterWeb.Models;

namespace SpotifyPlaylistSorterWeb.Services.Interfaces;

public interface IUserProfileService
{
    public Task<CurrentUserViewModel> GetCurrentUserViewModel();
}