namespace TrackAnalysisWorker.Models;

public class BpmPrediction
{
    public required double Value { get; set; }
    public required double Confidence { get; set; }
}