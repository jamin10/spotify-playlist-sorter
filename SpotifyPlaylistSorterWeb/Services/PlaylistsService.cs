using AutoMapper;
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

    public async Task<PlaylistsViewModel> GetPlaylistsViewModel()
    {
        var playlists = await _spotifyService.SpotifyClient.Playlists.CurrentUsers();
        var viewModel = new PlaylistsViewModel();
        return viewModel;
    }
}