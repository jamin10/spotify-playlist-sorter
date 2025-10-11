
using SpotifyAPI.Web;
using SpotifyPlaylistSorter.Business.Models.Cyanite;

namespace SpotifyPlaylistSorter.Business.Models;

public class AnalysedTrack
{
    public AnalysedTrack(FullTrack fullTrack, CyaniteTrack cyaniteTrack)
    {
        TrackDetails = new TrackModel
        {
            Name = fullTrack.Name,
            Artists = fullTrack.Artists.Select(a => new ArtistModel { Name = a.Name, SpotifyArtistId = a.Id ?? string.Empty }).ToList(),
            SpotifyTrackId = fullTrack.Id
        };

        AudioFeatures = new AudioFeatures
        {
            EnergyLevel = cyaniteTrack.Data.SpotifyTrack.audioAnalysisV6.Result.EnergyLevel,
            EnergyDynamics = cyaniteTrack.Data.SpotifyTrack.audioAnalysisV6.Result.EnergyDynamics,
            BpmPrediction = cyaniteTrack.Data.SpotifyTrack.audioAnalysisV6.Result.BpmPrediction,
            BpmRangeAdjusted = cyaniteTrack.Data.SpotifyTrack.audioAnalysisV6.Result.BpmRangeAdjusted
        };
    }

    public TrackModel TrackDetails { get; set; }

    public AudioFeatures AudioFeatures { get; set; }
}