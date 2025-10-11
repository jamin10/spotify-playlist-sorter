namespace SpotifyPlaylistSorter.Business.Models.Cyanite;

public class BpmPrediction
{
    public required double Value { get; set; }
    public required double Confidence { get; set; }
}