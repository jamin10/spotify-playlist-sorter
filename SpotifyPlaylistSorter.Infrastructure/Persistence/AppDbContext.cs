using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistSorter.Domain.Models;

namespace SpotifyPlaylistSorter.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Playlist> Playlists { get; set; }
    public DbSet<Track> Tracks { get; set; }
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Album> Albums { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Playlist>()
            .HasIndex(p => p.SpotifyPlaylistId)
            .IsUnique();

        modelBuilder.Entity<Track>()
            .HasIndex(t => t.SpotifyTrackId)
            .IsUnique();

        modelBuilder.Entity<Artist>()
            .HasIndex(a => a.SpotifyArtistId)
            .IsUnique();

        modelBuilder.Entity<Album>()
            .HasIndex(a => a.SpotifyId)
            .IsUnique();

        // Playlist <-> Track many-to-many
        modelBuilder.Entity<Playlist>()
            .HasMany(p => p.Tracks)
            .WithMany(t => t.Playlists)
            .UsingEntity("PlaylistTrack");

        // Track <-> Artist many-to-many
        modelBuilder.Entity<Track>()
            .HasMany(t => t.Artists)
            .WithMany(a => a.Tracks)
            .UsingEntity("TrackArtist");

        // Album <-> Artist many-to-many
        modelBuilder.Entity<Album>()
            .HasMany(a => a.Artists)
            .WithMany(ar => ar.Albums)
            .UsingEntity("AlbumArtist");

        // AudioFeatures as owned entity (columns on Tracks table)
        modelBuilder.Entity<Track>()
            .OwnsOne(t => t.AudioFeatures);
    }
}
