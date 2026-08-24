using System.Text.Json;
using SpotifyPlaylistSorter.Business.Models;
using SpotifyPlaylistSorter.Common.Models.QueueMessages;

namespace SpotifyPlaylistSorter.Business.Services;

public class PlaylistsService : IPlaylistsService
{
    private readonly ISpotifyUserContext _spotifyUserContext;
    private readonly IMessageBroker _messageBroker;

    public PlaylistsService(
        ISpotifyUserContext spotifyUserContext,
        IMessageBroker messageBroker)
    {
        _spotifyUserContext = spotifyUserContext;
        _messageBroker = messageBroker;
    }

    /// <inheritdoc />
    public async Task<PlaylistsViewModel> GetPlaylistsViewModel(int page, int pageSize)
    {
        var viewModel = new PlaylistsViewModel(page, pageSize) { IsLoggedIn = true };

        var playlists = await _spotifyUserContext.GetCurrentUserPlaylistsAsync(page, pageSize);
        playlists.Action = viewModel.Playlists.Action;
        viewModel.Playlists = playlists;

        return viewModel;
    }

    public async Task<FullPlaylistViewModel> GetFullPlaylistViewModel(string playlistId, int page, int pageSize)
    {
        var viewModel = new FullPlaylistViewModel(playlistId, page, pageSize) { IsLoggedIn = true };

        var tracks = await _spotifyUserContext.GetPlaylistItemsAsync(playlistId, page, pageSize);
        tracks.Action = viewModel.Tracks.Action;
        tracks.RouteValues = viewModel.Tracks.RouteValues;
        viewModel.Tracks = tracks;

        return viewModel;
    }

    public async Task<bool> AnalysePlaylist(string id)
    {
        var allTrackIds = await _spotifyUserContext.GetAllTrackIdsAsync(id);

        await QueueTracksForAnalysis(id, allTrackIds);

        return true;
    }

    private async Task QueueTracksForAnalysis(string playlistId, List<string> allTrackIds)
    {
        for (int i = 0; i < allTrackIds.Count; i += 10)
        {
            var batch = allTrackIds.Skip(i).Take(10).ToList();
            var message = new AnalysePlaylist(playlistId, batch);
            await _messageBroker.PublishAsync(JsonSerializer.Serialize(message));
        }
    }
}
