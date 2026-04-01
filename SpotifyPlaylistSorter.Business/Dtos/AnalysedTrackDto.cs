
using SpotifyAPI.Web;
using SpotifyPlaylistSorter.Business.Models.Cyanite;

namespace SpotifyPlaylistSorter.Business.Dtos;

public class AnalysedTrackDto
{
    public AnalysedTrackDto(FullTrack fullTrack, CyaniteTrack cyaniteTrack)
    {
        Title = fullTrack.Name;
        Artists = fullTrack.Artists.Select(a => new ArtistDto { Name = a.Name, SpotifyArtistId = a.Id ?? string.Empty }).ToList();
        SpotifyTrackId = fullTrack.Id;
        Album = new AlbumModelDto
        {
            SpotifyId = fullTrack.Album.Id,
            Name = fullTrack.Album.Name,
            Artists = fullTrack.Album.Artists.Select(a => new ArtistDto { Name = a.Name, SpotifyArtistId = a.Id ?? string.Empty }).ToList()
        };
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