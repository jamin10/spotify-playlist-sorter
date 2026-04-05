using SpotifyAPI.Web;
using SpotifyPlaylistSorter.Business.Dtos;

namespace SpotifyPlaylistSorter.Business.Services;

public interface ISpotifyService
{
    public SpotifyClient? SpotifyClient { get; set; }

    public Uri GetLoginUri();

    public Task CreateSpotifyClient(string code);

    public Task AuthenticateWithClientCredentialsAsync();

    public Task<SpotifyTrackDto> GetTrackAsync(string trackId);

    public Task<SpotifyPlaylistDto> GetPlaylistAsync(string playlistId);
}
