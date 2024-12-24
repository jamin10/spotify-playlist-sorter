using AutoMapper;
using SpotifyAPI.Web;
using SpotifyPlaylistSorterWeb.Models;
using SpotifyPlaylistSorterWeb.Services.Interfaces;

namespace SpotifyPlaylistSorterWeb.Services;

public class PlaylistsService : IPlaylistsService
{
    private readonly ISpotifyService _spotifyService;
    private readonly IMapper _mapper;

    public PlaylistsService(ISpotifyService spotifyService, IMapper mapper)
    {
        _spotifyService = spotifyService;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<PlaylistsViewModel> GetPlaylistsViewModel()
    {
        var viewModel = new PlaylistsViewModel();
                if (_spotifyService.SpotifyClient == null)
        {
            return viewModel;
        }
        viewModel.IsLoggedIn = true;

        var request = new PlaylistCurrentUsersRequest { Limit = 10 };
        var playlists = await _spotifyService.SpotifyClient.Playlists.CurrentUsers(request);
        // var audioFeatures = await _spotifyService.SpotifyClient.Tracks.GetAudioFeatures("11dFghVXANMlKmJXsNCbNl");
        if (playlists != null)
        {
            _mapper.Map(playlists, viewModel);
        }
        
        return viewModel;
    }
}