namespace SpotifyPlaylistSorter.Business.Services;

/// <summary>
/// Web-only: manages the current request's Spotify login state. Kept separate from
/// ISpotifyCredentialProvider so the login-gate filter doesn't depend on the shared seam
/// the Worker also adapts to.
/// </summary>
public interface ISpotifySessionAuth
{
    bool IsAuthenticated { get; }

    Uri GetLoginUri();

    Task SignInAsync(string code);

    void SignOut();
}
