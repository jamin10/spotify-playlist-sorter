namespace SpotifyPlaylistSorter.Business.Dtos;

public class AudioFeatures
{
    public string? EnergyLevel { get; set; }
    public string? EnergyDynamics { get; set; }
    public int? Bpm { get; set; }
    public int? BpmRangeAdjusted { get; set; }
}