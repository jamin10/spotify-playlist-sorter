using System.Text.Json;
using SpotifyPlaylistSorterWeb.Models.QueueMessages;
using TrackAnalysisWorker.Clients.Interfaces;
using TrackAnalysisWorker.Models;

namespace TrackAnalysisWorker.Services;

public class TrackAnalyserService : IAnalyserService
{
    private readonly ICyaniteClient _cyaniteClient;

    public TrackAnalyserService(ICyaniteClient cyaniteClient)
    {
        _cyaniteClient = cyaniteClient;
    }
    public async Task<bool> Analyse(AnalysePlaylist message)
    {
        var query = @"
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

        //var spotifyTrackId = "6suVCaWE1ssKwdnLJyjyxy";

        var variables = new { id = message.TrackIds.First() };

        var response = await _cyaniteClient.GetAsync(query, variables);

        var result = JsonSerializer.Deserialize<CyaniteTrack>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return true;
    }
}