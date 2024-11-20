using AutoMapper;
using SpotifyPlaylistSorterWeb.Models;
using SpotifyPlaylistSorterWeb.Services.Interfaces;

namespace SpotifyPlaylistSorterWeb.Services;

public class UserProfileService : IUserProfileService
{
    private readonly ISpotifyService _spotifyService;

    private readonly IMapper _mapper;

    public UserProfileService(ISpotifyService spotifyService, IMapper mapper)
    {
        _spotifyService = spotifyService;
        _mapper = mapper;
    }
    public async Task<CurrentUserViewModel> GetCurrentUserViewModel()
    {
        var viewModel = new CurrentUserViewModel{};
        if (_spotifyService.SpotifyClient == null)
        {
            return viewModel;
        }
        
        viewModel.IsLoggedIn = true;
        var userProfile = await _spotifyService.SpotifyClient.UserProfile.Current();
        if (userProfile != null)
        {
            viewModel = _mapper.Map<CurrentUserViewModel>(userProfile);
        }
        return viewModel;
    }
}