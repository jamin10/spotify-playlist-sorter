using DomainModels = SpotifyPlaylistSorter.Domain.Models;

namespace SpotifyPlaylistSorter.Business.Services;

public interface ITrackStore
{
    Task<List<DomainModels.Track>> GetExistingTracksAsync(IEnumerable<string> spotifyTrackIds);
    Task<DomainModels.Playlist?> FindPlaylistAsync(string spotifyPlaylistId);
    Task<DomainModels.Artist?> FindArtistAsync(string spotifyArtistId);
    Task<DomainModels.Album?> FindAlbumAsync(string spotifyAlbumId);
    void Add<T>(T entity) where T : class;
    Task SaveChangesAsync();
}
