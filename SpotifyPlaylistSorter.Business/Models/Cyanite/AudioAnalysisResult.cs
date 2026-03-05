using System.Text.Json.Serialization;

namespace SpotifyPlaylistSorter.Business.Models.Cyanite;

public class AudioAnalysisResult
{
    [JsonPropertyName("energyLevel")]
    public required string EnergyLevel { get; set; }
    
    [JsonPropertyName("energyDynamics")]
    public required string EnergyDynamics { get; set; }
    
    [JsonPropertyName("bpmPrediction")]
    public required BpmPrediction BpmPrediction { get; set; }
    
    [JsonPropertyName("bpmRangeAdjusted")]
    public required int? BpmRangeAdjusted { get; set; }
}