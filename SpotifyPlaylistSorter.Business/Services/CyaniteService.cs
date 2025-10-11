using System.Text.Json;
using SpotifyPlaylistSorter.Business.Clients.Interfaces;
using SpotifyPlaylistSorter.Business.Models.Cyanite;

namespace SpotifyPlaylistSorter.Business.Services;

public class CyaniteService : ICyaniteService
{
    private readonly ICyaniteClient _cyaniteClient;

    public CyaniteService(ICyaniteClient cyaniteClient)
    {
        _cyaniteClient = cyaniteClient;
    }

    public async Task<CyaniteTrack> GetTrackAnalysis(string trackId)
    {
        var variables = new { id = trackId };
        var response = await _cyaniteClient.GetAsync(GetCyaniteQuery(), variables);

        var result = JsonSerializer.Deserialize<CyaniteTrack>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result;
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