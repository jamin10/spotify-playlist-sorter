namespace SpotifyPlaylistSorter.Business.Dtos;

public class AlbumModelDto
{
    public string SpotifyId { get; set; }

    public string Name { get; set; }

    public List<ArtistDto> Artists { get; set; }
}
