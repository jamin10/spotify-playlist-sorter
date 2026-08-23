namespace SpotifyPlaylistSorter.Domain.Models;

public class TrackAudioFeatures
{
    public TrackAudioFeatures(
        string? energyLevel,
        string? energyDynamics,
        int? bpm,
        int? bpmRangeAdjusted)
    {
        EnergyLevel = energyLevel;
        EnergyDynamics = energyDynamics;
        Bpm = bpm;
        BpmRangeAdjusted = bpmRangeAdjusted;
    }

    public string? EnergyLevel { get; init; }
    public string? EnergyDynamics { get; init; }
    public int? Bpm { get; init; }
    public int? BpmRangeAdjusted { get; init; }
}
