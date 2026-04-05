using SpotifyPlaylistSorter.Business.Models.Cyanite;

namespace SpotifyPlaylistSorter.Business.Dtos;

public class AnalysedTrackDto
{
    public AnalysedTrackDto(SpotifyTrackDto spotifyTrack, CyaniteTrack cyaniteTrack)
    {
        Title = spotifyTrack.Title;
        Artists = spotifyTrack.Artists;
        SpotifyTrackId = spotifyTrack.SpotifyTrackId;
        Album = spotifyTrack.Album;
        AudioFeatures = new AudioFeatures
        {
            EnergyLevel = cyaniteTrack.Data.SpotifyTrack.AudioAnalysisV6.Result?.EnergyLevel,
            EnergyDynamics = cyaniteTrack.Data.SpotifyTrack.AudioAnalysisV6.Result?.EnergyDynamics,
            Bpm = (int)(cyaniteTrack.Data.SpotifyTrack.AudioAnalysisV6.Result?.BpmPrediction?.Value ?? 0),
            BpmRangeAdjusted = cyaniteTrack.Data.SpotifyTrack.AudioAnalysisV6.Result?.BpmRangeAdjusted
        };
    }

    public string Title { get; set; }

    public List<ArtistDto> Artists { get; set; }

    public string SpotifyTrackId { get; set; }

    public AlbumModelDto Album { get; set; }

    public AudioFeatures AudioFeatures { get; set; }
}
