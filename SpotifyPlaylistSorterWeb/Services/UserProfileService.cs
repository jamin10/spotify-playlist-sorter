using SpotifyPlaylistSorterWeb.Models;
using SpotifyPlaylistSorterWeb.Services.Interfaces;

namespace SpotifyPlaylistSorterWeb.Services;

public class UserProfileService : IUserProfileService
{
    private readonly ISpotifyService _spotifyService;

    public UserProfileService(ISpotifyService spotifyService)
    {
        _spotifyService = spotifyService;
    }
    public async Task<CurrentUserViewModel> GetCurrentUserViewModel()
    {
        var viewModel = new CurrentUserViewModel{};
        if (_spotifyService.SpotifyClient != null)
        {
            viewModel.IsLoggedIn = true;
            var userProfile = await _spotifyService.SpotifyClient.UserProfile.Current();
            if (userProfile != null)
            {
                viewModel.UserProfile = userProfile;
            }
        }
        return viewModel;
    }
}