namespace TrackAnalysisWorker.Models;

public class SpotifyTrack
{
    public required string __typename { get; set; }
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required AudioAnalysisV6 audioAnalysisV6 { get; set; }
}