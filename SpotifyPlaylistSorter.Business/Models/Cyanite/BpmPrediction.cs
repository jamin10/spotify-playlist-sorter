using System.Text.Json.Serialization;

namespace SpotifyPlaylistSorter.Business.Models.Cyanite;

public class BpmPrediction
{
    [JsonPropertyName("value")]
    public required double Value { get; set; }
    
    [JsonPropertyName("confidence")]
    public required double Confidence { get; set; }
}