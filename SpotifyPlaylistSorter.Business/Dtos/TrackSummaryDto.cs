namespace SpotifyPlaylistSorter.Business.Dtos;

public class TrackSummaryDto
{
    public required string Name { get; set; }
    public required List<ArtistDto> Artists { get; set; }
    public required string SpotifyTrackId { get; set; }
}
