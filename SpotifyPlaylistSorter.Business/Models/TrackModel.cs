using SpotifyAPI.Web;

namespace SpotifyPlaylistSorter.Business.Models;

public class TrackModel
{
    public TrackModel() { }
    
    public TrackModel(FullTrack fullTrack)
    {
        Name = fullTrack.Name;
        Artists = fullTrack.Artists.Select(a => new ArtistModel { Name = a.Name, SpotifyArtistId = a.Id ?? string.Empty }).ToList();
        SpotifyTrackId = fullTrack.Id;
    }

    public required string Name { get; set; }

    public required List<ArtistModel> Artists { get; set; }

    public required string SpotifyTrackId { get; set; }
}