using SpotifyPlaylistSorter.Domain.Models;

namespace SpotifyPlaylistSorter.Domain.Repositories;

public interface ITrackStore
{
    Task<List<Track>> GetExistingTracksAsync(IEnumerable<string> spotifyTrackIds);
    Task<Playlist?> FindPlaylistAsync(string spotifyPlaylistId);
    Task<Artist?> FindArtistAsync(string spotifyArtistId);
    Task<Album?> FindAlbumAsync(string spotifyAlbumId);
    void Add<T>(T entity) where T : class;
    Task SaveChangesAsync();
}
