using System.Text.Json;
using SpotifyPlaylistSorter.Common.Models.QueueMessages;
using SpotifyPlaylistSorter.Business.Clients.Interfaces;
using SpotifyPlaylistSorter.Business.Models.Cyanite;
using SpotifyPlaylistSorter.Business.Models;

namespace SpotifyPlaylistSorter.Business.Services;

public class PlaylistAnalyserService : IAnalyserService
{
    private readonly ICyaniteService _cyaniteService;
    private readonly ISpotifyService _spotifyService;

    public PlaylistAnalyserService(ICyaniteService cyaniteService, ISpotifyService spotifyService)
    {
        _cyaniteService = cyaniteService;
        _spotifyService = spotifyService;
        _spotifyService.AuthenticateWithClientCredentialsAsync();
    }
    public async Task<bool> Analyse(AnalysePlaylist message)
    {
        foreach (var trackId in message.TrackIds)
        {
            var cyaniteTrack = await _cyaniteService.GetTrackAnalysis(trackId);

            var spotifyTrack = await _spotifyService.SpotifyClient.Tracks.Get(trackId);

            var AnalysedTrack = new AnalysedTrack(spotifyTrack, cyaniteTrack);
        }

        return true;
    }
}