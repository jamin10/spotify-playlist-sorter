using System.Threading.Tasks;
using TrackAnalysisWorker.Clients.Interfaces;

namespace TrackAnalysisWorker.Services;

public class TrackAnalyserService : IAnalyserService
{
    private readonly ICyaniteClient _cyaniteClient;

    public TrackAnalyserService(ICyaniteClient cyaniteClient)
    {
        _cyaniteClient = cyaniteClient;
    }
    public async Task<bool> Analyse(string id)
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

        var spotifyTrackId = "6suVCaWE1ssKwdnLJyjyxy";

        var variables = new { id = spotifyTrackId };

        var response = await _cyaniteClient.GetAsync(query, variables);

        return true;
    }
}