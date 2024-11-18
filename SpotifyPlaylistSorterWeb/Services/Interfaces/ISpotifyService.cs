using SpotifyAPI.Web;

namespace SpotifyPlaylistSorterWeb.Services.Interfaces;

public interface ISpotifyService
{
    public SpotifyClient? SpotifyClient { get; set; }

    public Uri GetLoginUri();

    public Task CreateSpotifyClient(string code);
}
