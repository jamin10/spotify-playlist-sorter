using SpotifyPlaylistSorter.Business.Models;

namespace SpotifyPlaylistSorter.Business.Services;

public interface IUserProfileService
{
    /// <summary>
    /// Gets the view model for the home page.
    /// </summary>
    /// <returns>A view model.</returns>
    public Task<CurrentUserViewModel> GetCurrentUserViewModel();
}