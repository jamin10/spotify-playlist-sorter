using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web;

namespace SpotifyPlaylistSorter.Business.Services;

/// <summary>
/// Session-backed credential adapter used by the Web app. Registered scoped: takes ISession
/// directly (not IHttpContextAccessor) so it can be unit tested with a fake session. Persists
/// access token + refresh token + expiry as one JSON session key and re-persists whenever
/// AuthorizationCodeAuthenticator refreshes the access token mid-request.
/// </summary>
public class SessionSpotifyCredentialProvider : ISpotifyCredentialProvider, ISpotifySessionAuth
{
    private const string SessionKey = "spotify_token";

    private readonly ISession _session;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly Uri _redirectUri;
    private SpotifyClient? _client;

    public SessionSpotifyCredentialProvider(ISession session, IConfiguration configuration)
    {
        _session = session;
        _clientId = configuration["SpotifyCredentials:ClientId"]
            ?? throw new InvalidOperationException("Missing SpotifyCredentials:ClientId");
        _clientSecret = configuration["SpotifyCredentials:ClientSecret"]
            ?? throw new InvalidOperationException("Missing SpotifyCredentials:ClientSecret");
        _redirectUri = new Uri(configuration["SpotifyCredentials:RedirectUri"]
            ?? throw new InvalidOperationException("Missing SpotifyCredentials:RedirectUri"));
    }

    public bool IsAuthenticated => ReadToken() is not null;

    public Uri GetLoginUri()
    {
        var loginRequest = new LoginRequest(_redirectUri, _clientId, LoginRequest.ResponseType.Code)
        {
            Scope = new[] { Scopes.PlaylistReadPrivate, Scopes.PlaylistReadCollaborative }
        };
        return loginRequest.ToUri();
    }

    public async Task SignInAsync(string code)
    {
        var response = await new OAuthClient().RequestToken(
            new AuthorizationCodeTokenRequest(_clientId, _clientSecret, code, _redirectUri));
        WriteToken(response);
    }

    public void SignOut()
    {
        _session.Remove(SessionKey);
        _client = null;
    }

    public Task<SpotifyClient> GetClientAsync()
    {
        if (_client is not null)
        {
            return Task.FromResult(_client);
        }

        var stored = ReadToken() ?? throw new InvalidOperationException(
            "No Spotify session found. Callers must be gated behind [RequireSpotifyLogin].");

        var initialToken = new AuthorizationCodeTokenResponse
        {
            AccessToken = stored.AccessToken,
            RefreshToken = stored.RefreshToken,
            TokenType = stored.TokenType,
            ExpiresIn = stored.ExpiresIn,
            Scope = stored.Scope ?? string.Empty,
            CreatedAt = stored.CreatedAt
        };

        var authenticator = new AuthorizationCodeAuthenticator(_clientId, _clientSecret, initialToken);
        authenticator.TokenRefreshed += (_, refreshedToken) => WriteToken(refreshedToken);

        _client = new SpotifyClient(SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator));
        return Task.FromResult(_client);
    }

    private SpotifySessionToken? ReadToken()
    {
        if (!_session.TryGetValue(SessionKey, out var bytes))
        {
            return null;
        }

        var json = System.Text.Encoding.UTF8.GetString(bytes);
        return JsonSerializer.Deserialize<SpotifySessionToken>(json);
    }

    private void WriteToken(AuthorizationCodeTokenResponse response)
    {
        var token = new SpotifySessionToken(
            response.AccessToken,
            response.RefreshToken,
            response.TokenType,
            response.ExpiresIn,
            response.Scope,
            response.CreatedAt);
        var json = JsonSerializer.Serialize(token);
        _session.Set(SessionKey, System.Text.Encoding.UTF8.GetBytes(json));
    }

    private record SpotifySessionToken(
        string AccessToken,
        string RefreshToken,
        string TokenType,
        int ExpiresIn,
        string? Scope,
        DateTime CreatedAt);
}
