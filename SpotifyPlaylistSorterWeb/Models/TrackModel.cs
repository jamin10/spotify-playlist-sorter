namespace SpotifyPlaylistSorterWeb.Models;

public class TrackModel
{
    public required string Name { get; set; }

    public required List<ArtistModel> Artists { get; set; }

    public required string SpotifyTrackId { get; set; }
}