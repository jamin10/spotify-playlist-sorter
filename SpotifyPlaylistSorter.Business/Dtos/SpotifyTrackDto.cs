namespace SpotifyPlaylistSorter.Business.Dtos;

public record SpotifyTrackDto(
    string SpotifyTrackId,
    string Title,
    AlbumModelDto Album,
    List<ArtistDto> Artists
);
