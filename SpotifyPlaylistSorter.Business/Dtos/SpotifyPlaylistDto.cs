namespace SpotifyPlaylistSorter.Business.Dtos;

public record SpotifyPlaylistDto(
    string SpotifyPlaylistId,
    string Name,
    string? Description,
    string? ImageUrl
);
