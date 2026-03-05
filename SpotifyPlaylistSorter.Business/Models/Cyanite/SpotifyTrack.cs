using System.Text.Json.Serialization;

namespace SpotifyPlaylistSorter.Business.Models.Cyanite;

public class SpotifyTrack
{
    public required string __typename { get; set; }
    
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    
    [JsonPropertyName("title")]
    public required string Title { get; set; }
    
    [JsonPropertyName("audioAnalysisV6")]
    public required AudioAnalysisV6 AudioAnalysisV6 { get; set; }
}