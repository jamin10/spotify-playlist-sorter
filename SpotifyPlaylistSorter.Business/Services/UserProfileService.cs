using AutoMapper;
using SpotifyPlaylistSorter.Business.Models;

namespace SpotifyPlaylistSorter.Business.Services;

public class UserProfileService : IUserProfileService
{
    private readonly ISpotifySessionAuth _sessionAuth;

    private readonly ISpotifyUserContext _spotifyUserContext;

    private readonly IMapper _mapper;

    public UserProfileService(ISpotifySessionAuth sessionAuth, ISpotifyUserContext spotifyUserContext, IMapper mapper)
    {
        _sessionAuth = sessionAuth;
        _spotifyUserContext = spotifyUserContext;
        _mapper = mapper;
    }

    public async Task<CurrentUserViewModel> GetCurrentUserViewModel()
    {
        var viewModel = new CurrentUserViewModel { };
        if (!_sessionAuth.IsAuthenticated)
        {
            return viewModel;
        }

        viewModel.IsLoggedIn = true;
        var userProfile = await _spotifyUserContext.GetCurrentUserAsync();
        _mapper.Map(userProfile, viewModel);
        return viewModel;
    }
}
