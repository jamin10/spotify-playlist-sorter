namespace SpotifyPlaylistSorter.Business.Dtos;

public class PlaylistDto
{    
    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public string? TracksUrl { get; set; }

    public int? TracksTotal { get; set; }
    
    public string? SpotifyPlaylistId { get; set; }

    public List<AnalysedTrackDto>? Tracks { get; set; }
}