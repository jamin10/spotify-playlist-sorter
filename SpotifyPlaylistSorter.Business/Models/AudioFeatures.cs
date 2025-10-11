using SpotifyPlaylistSorter.Business.Models.Cyanite;

namespace SpotifyPlaylistSorter.Business.Models;

public class AudioFeatures
{
    public string? EnergyLevel { get; set; }
    public string? EnergyDynamics { get; set; }
    public BpmPrediction? BpmPrediction { get; set; }
    public int BpmRangeAdjusted { get; set; }
}