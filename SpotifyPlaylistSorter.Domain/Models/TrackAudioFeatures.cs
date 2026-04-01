namespace SpotifyPlaylistSorter.Domain.Models;

public class TrackAudioFeatures
{
    public string? EnergyLevel { get; set; }
    public string? EnergyDynamics { get; set; }
    public int? Bpm { get; set; }
    public int? BpmRangeAdjusted { get; set; }
}
