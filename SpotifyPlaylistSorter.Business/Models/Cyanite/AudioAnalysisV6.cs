using System.Text.Json.Serialization;

namespace SpotifyPlaylistSorter.Business.Models.Cyanite;

public class AudioAnalysisV6
{
    public required string __typename { get; set; }
    
    [JsonPropertyName("result")]
    public AudioAnalysisResult? Result { get; set; }
}