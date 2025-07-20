using System.Text.Json;
using AutoMapper;
using Azure;
using SpotifyAPI.Web;
using SpotifyPlaylistSorterWeb.Models;
using SpotifyPlaylistSorterWeb.Models.QueueMessages;
using SpotifyPlaylistSorterWeb.Services.Interfaces;

namespace SpotifyPlaylistSorterWeb.Services.Implementations;

public class PlaylistsService : IPlaylistsService
{
    private readonly ISpotifyService _spotifyService;
    private readonly IMapper _mapper;
    private readonly IMessageQueueService _messageQueueService;

    public PlaylistsService(
        ISpotifyService spotifyService,
        IMapper mapper,
        IMessageQueueService messageQueueService)
    {
        _spotifyService = spotifyService;
        _mapper = mapper;
        _messageQueueService = messageQueueService;
    }

    /// <inheritdoc />
    public async Task<PlaylistsViewModel> GetPlaylistsViewModel(int page, int pageSize)
    {
        var viewModel = new PlaylistsViewModel(page, pageSize);
        if (_spotifyService.SpotifyClient == null)
        {
            return viewModel;
        }
        viewModel.IsLoggedIn = true;

        var request = new PlaylistCurrentUsersRequest
        {
            Limit = pageSize,
            Offset = (page - 1) * pageSize
        };

        var playlists = await _spotifyService.SpotifyClient.Playlists.CurrentUsers(request);
        if (playlists != null)
        {
            _mapper.Map(playlists, viewModel.Playlists);
        }

        return viewModel;
    }

    public async Task<FullPlaylistViewModel> GetFullPlaylistViewModel(string playlistId, int page, int pageSize)
    {
        var viewModel = new FullPlaylistViewModel(playlistId, page, pageSize);
        if (_spotifyService.SpotifyClient == null)
        {
            return viewModel;
        }
        viewModel.IsLoggedIn = true;

        var request = new PlaylistGetItemsRequest
        {
            Limit = pageSize,
            Offset = (page - 1) * pageSize,
        };

        var playlist = await _spotifyService.SpotifyClient.Playlists.GetItems(playlistId, request);

        if (playlist != null)
        {
            _mapper.Map(playlist, viewModel.Tracks);
        }

        return viewModel;
    }

    public async Task<bool> AnalysePlaylist(string id)
    {
        if (_spotifyService.SpotifyClient == null) { return false; }

        var request = new PlaylistGetItemsRequest
        {
            Limit = 10,
            Offset = 0
        };

        var tracks = await _spotifyService.SpotifyClient.Playlists.GetItems(id, request);
        var trackIds = tracks.Items.Select(t => ((FullTrack)t.Track).Id).ToList();
        var message = new AnalysePlaylist(id, trackIds);

        await _messageQueueService.SendMessage(JsonSerializer.Serialize(message));

        while (tracks.Next != null)
        {
            request.Offset += 10;
            tracks = await _spotifyService.SpotifyClient.Playlists.GetItems(id, request);
        }

        return true;
    }
        
}