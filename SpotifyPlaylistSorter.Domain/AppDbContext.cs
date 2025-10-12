using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistSorter.Domain.Models;

namespace SpotifyPlaylistSorter.Domain
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Playlist> Playlists { get; set; }
        // Add other DbSets as needed

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configure entity mappings if needed
        }
    }
}
