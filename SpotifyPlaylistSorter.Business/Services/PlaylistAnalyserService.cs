using System.Text.Json;
using SpotifyPlaylistSorter.Common.Models.QueueMessages;
using SpotifyPlaylistSorter.Business.Clients.Interfaces;
using SpotifyPlaylistSorter.Business.Models.Cyanite;

namespace SpotifyPlaylistSorter.Business.Services;

public class PlaylistAnalyserService : IAnalyserService
{
    private readonly ICyaniteClient _cyaniteClient;
    //private readonly ISpotifyService _spotifyService;

    public PlaylistAnalyserService(ICyaniteClient cyaniteClient)
    {
        _cyaniteClient = cyaniteClient;
        //_spotifyService = spotifyService;
    }
    public async Task<bool> Analyse(AnalysePlaylist message)
    {
        foreach (var trackId in message.TrackIds)
        {
            var variables = new { id = trackId };
            var response = await _cyaniteClient.GetAsync(GetCyaniteQuery(), variables);

            var result = JsonSerializer.Deserialize<CyaniteTrack>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });


        }



        return true;
    }

    private string GetCyaniteQuery()
    {
        return @"
            query SpotifyTrackQuery($id: ID!) {
                spotifyTrack(id: $id) {
                    __typename
                    ... on SpotifyTrackError {
                        message
                        }
                    ... on SpotifyTrack {
                        id
                        title
                        audioAnalysisV6 {
                            __typename
                            ... on AudioAnalysisV6Finished {
                                result {
                                    energyLevel
                                    energyDynamics
                                    bpmPrediction {
                                        value
                                        confidence
                                        }
                                    bpmRangeAdjusted
                                    }}}}}}";
    }
}