using System.Text.Json;
using AutoMapper;
using SpotifyAPI.Web;
using SpotifyPlaylistSorter.Business.Models;
using SpotifyPlaylistSorterWeb.Models.QueueMessages;

namespace SpotifyPlaylistSorter.Business.Services;

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
        if (_spotifyService.SpotifyClient == null) return false;

        var allTrackIds = GetAllTrackIds(id);

        await QueueTracksForAnalysis(id, allTrackIds);

        return true;
    }

    private List<string> GetAllTrackIds(string playlistId)
    {
        var allTrackIds = new List<string>();
        var request = new PlaylistGetItemsRequest { Limit = 100, Offset = 0 };
        Paging<PlaylistTrack<IPlayableItem>> tracks;

        do
        {
            tracks = _spotifyService.SpotifyClient.Playlists.GetItems(playlistId, request).Result;
            if (tracks?.Items == null) break;

            allTrackIds.AddRange(
                tracks.Items
                    .Select(t => t.Track as FullTrack)
                    .Where(ft => ft != null)
                    .Select(ft => ft.Id)
            );

            request.Offset += request.Limit;
        }
        while (tracks.Next != null);

        return allTrackIds;
    }

    private async Task QueueTracksForAnalysis(string playlistId, List<string> allTrackIds)
    {
        for (int i = 0; i < allTrackIds.Count; i += 10)
        {
            var batch = allTrackIds.Skip(i).Take(10).ToList();
            var message = new AnalysePlaylist(playlistId, batch);
            await _messageQueueService.SendMessage(JsonSerializer.Serialize(message));
        }
    }
        
}