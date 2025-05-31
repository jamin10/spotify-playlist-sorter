namespace TrackAnalysisWorker.Models;

public class AudioAnalysisResult
{
    public required string EnergyLevel { get; set; }
    public required string EnergyDynamics { get; set; }
    public required BpmPrediction BpmPrediction { get; set; }
    public required int BpmRangeAdjusted { get; set; }
}