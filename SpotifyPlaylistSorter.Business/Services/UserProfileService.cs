using AutoMapper;
using SpotifyPlaylistSorter.Business.Models;

namespace SpotifyPlaylistSorter.Business.Services;

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
            _mapper.Map(userProfile, viewModel);
        }
        return viewModel;
    }
}