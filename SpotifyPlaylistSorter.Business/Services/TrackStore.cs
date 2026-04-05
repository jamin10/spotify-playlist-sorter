using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistSorter.Domain;
using DomainModels = SpotifyPlaylistSorter.Domain.Models;

namespace SpotifyPlaylistSorter.Business.Services;

public class TrackStore : ITrackStore
{
    private readonly AppDbContext _db;

    public TrackStore(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<DomainModels.Track>> GetExistingTracksAsync(IEnumerable<string> spotifyTrackIds)
        => _db.Tracks
            .Include(t => t.Artists)
            .Include(t => t.Playlists)
            .Where(t => spotifyTrackIds.Contains(t.SpotifyTrackId))
            .ToListAsync();

    public Task<DomainModels.Playlist?> FindPlaylistAsync(string spotifyPlaylistId)
        => _db.Playlists
            .Include(p => p.Tracks)
            .FirstOrDefaultAsync(p => p.SpotifyPlaylistId == spotifyPlaylistId);

    public Task<DomainModels.Artist?> FindArtistAsync(string spotifyArtistId)
        => _db.Artists
            .FirstOrDefaultAsync(a => a.SpotifyArtistId == spotifyArtistId);

    public Task<DomainModels.Album?> FindAlbumAsync(string spotifyAlbumId)
        => _db.Albums
            .Include(a => a.Artists)
            .FirstOrDefaultAsync(a => a.SpotifyId == spotifyAlbumId);

    public void Add<T>(T entity) where T : class
        => _db.Set<T>().Add(entity);

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}
