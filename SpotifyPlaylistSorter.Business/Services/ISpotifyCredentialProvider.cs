using SpotifyAPI.Web;

namespace SpotifyPlaylistSorter.Business.Services;

/// <summary>
/// Produces an authenticated Spotify client. Two adapters: session-backed user auth (Web)
/// and client-credentials app auth (Worker). Never returns null - throws if a client can't
/// be produced, since callers are expected to be gated (e.g. by [RequireSpotifyLogin]) before
/// this is ever invoked in a state where that would happen.
/// </summary>
public interface ISpotifyCredentialProvider
{
    Task<SpotifyClient> GetClientAsync();
}
