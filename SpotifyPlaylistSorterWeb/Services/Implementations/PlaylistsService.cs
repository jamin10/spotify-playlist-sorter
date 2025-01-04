using AutoMapper;
using Azure;
using SpotifyAPI.Web;
using SpotifyPlaylistSorterWeb.Models;
using SpotifyPlaylistSorterWeb.Services.Interfaces;

namespace SpotifyPlaylistSorterWeb.Services.Implementations;

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
    public async Task<PlaylistsViewModel> GetPlaylistsViewModel(int page, int pageSize)
    {
        var viewModel = new PlaylistsViewModel();
        if (_spotifyService.SpotifyClient == null)
        {
            return viewModel;
        }
        viewModel.IsLoggedIn = true;

        var request = new PlaylistCurrentUsersRequest
        {
            Limit = pageSize,
            Offset = page * pageSize
        };

        var playlists = await _spotifyService.SpotifyClient.Playlists.CurrentUsers(request);
        if (playlists != null)
        {
            _mapper.Map(playlists, viewModel.Playlists);
        }

        return viewModel;
    }

    public async Task<FullPlaylistViewModel> GetFullPlaylistViewModel(string playlistId)
    {
        var viewModel = new FullPlaylistViewModel();
        if (_spotifyService.SpotifyClient == null)
        {
            return viewModel;
        }
        viewModel.IsLoggedIn = true;

        var playlist = await _spotifyService.SpotifyClient.Playlists.GetItems(playlistId);

        if (playlist != null)
        {
            _mapper.Map(playlist, viewModel.Tracks);
        }

        return viewModel;
    }
}