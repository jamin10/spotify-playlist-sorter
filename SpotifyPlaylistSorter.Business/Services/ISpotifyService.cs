using SpotifyAPI.Web;

namespace SpotifyPlaylistSorter.Business.Services;

public interface ISpotifyService
{
    public SpotifyClient? SpotifyClient { get; set; }

    public Uri GetLoginUri();

    public Task CreateSpotifyClient(string code);

    public Task AuthenticateWithClientCredentialsAsync();
}
