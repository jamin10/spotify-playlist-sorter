using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistSorter.Domain.Models;
using SpotifyPlaylistSorter.Domain.Repositories;

namespace SpotifyPlaylistSorter.Infrastructure.Persistence;

public class TrackStore : ITrackStore
{
    private readonly AppDbContext _db;

    public TrackStore(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Track>> GetExistingTracksAsync(IEnumerable<string> spotifyTrackIds)
        => _db.Tracks
            .Include(t => t.Artists)
            .Include(t => t.Playlists)
            .Where(t => spotifyTrackIds.Contains(t.SpotifyTrackId))
            .ToListAsync();

    public Task<Playlist?> FindPlaylistAsync(string spotifyPlaylistId)
        => _db.Playlists
            .Include(p => p.Tracks)
            .FirstOrDefaultAsync(p => p.SpotifyPlaylistId == spotifyPlaylistId);

    public Task<Artist?> FindArtistAsync(string spotifyArtistId)
        => _db.Artists
            .FirstOrDefaultAsync(a => a.SpotifyArtistId == spotifyArtistId);

    public Task<Album?> FindAlbumAsync(string spotifyAlbumId)
        => _db.Albums
            .Include(a => a.Artists)
            .FirstOrDefaultAsync(a => a.SpotifyId == spotifyAlbumId);

    public void Add<T>(T entity) where T : class
        => _db.Set<T>().Add(entity);

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}
