using SpotifyAPI.Web;

namespace SpotifyPlaylistSorterWeb;

public interface ISpotifyService
{
    public SpotifyClient? Spotify { get; set; }

    public Uri GetLoginUri();

    public Task<SpotifyClient> CreateSpotifyClient(string code);
}
