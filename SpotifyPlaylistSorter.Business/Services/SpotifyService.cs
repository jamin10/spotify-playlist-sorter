using SpotifyPlaylistSorter.Business.Dtos;

namespace SpotifyPlaylistSorter.Business.Services;

public class SpotifyService : ISpotifyService
{
    private readonly ISpotifyCredentialProvider _credentialProvider;

    public SpotifyService(ISpotifyCredentialProvider credentialProvider)
    {
        _credentialProvider = credentialProvider;
    }

    public async Task<SpotifyTrackDto> GetTrackAsync(string trackId)
    {
        var client = await _credentialProvider.GetClientAsync();
        var track = await client.Tracks.Get(trackId);
        return new SpotifyTrackDto(
            SpotifyTrackId: track.Id,
            Title: track.Name,
            Album: new AlbumModelDto
            {
                SpotifyId = track.Album.Id,
                Name = track.Album.Name,
                Artists = track.Album.Artists
                    .Select(a => new ArtistDto { SpotifyArtistId = a.Id ?? string.Empty, Name = a.Name })
                    .ToList()
            },
            Artists: track.Artists
                .Select(a => new ArtistDto { SpotifyArtistId = a.Id ?? string.Empty, Name = a.Name })
                .ToList()
        );
    }

    public async Task<SpotifyPlaylistDto> GetPlaylistAsync(string playlistId)
    {
        var client = await _credentialProvider.GetClientAsync();
        var playlist = await client.Playlists.Get(playlistId);
        return new SpotifyPlaylistDto(
            SpotifyPlaylistId: playlistId,
            Name: playlist.Name ?? string.Empty,
            Description: playlist.Description,
            ImageUrl: playlist.Images?.FirstOrDefault()?.Url
        );
    }
}
