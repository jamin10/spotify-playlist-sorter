using SpotifyAPI.Web;
using SpotifyPlaylistSorterWeb.Services.Interfaces;
using SpAPI = SpotifyAPI.Web;

namespace SpotifyPlaylistSorterWeb.Services;

public class SpotifyService : ISpotifyService
{
    private readonly IConfiguration _configuration;

    private readonly IHttpContextAccessor _httpContextAccessor;
    
    private readonly string? ClientId;

    private readonly string? ClientSecret;

    private readonly Uri? RedirectUri;

    public SpotifyClient? SpotifyClient { get; set; }

    public SpotifyService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        
        ClientId = _configuration["SpotifyCredentials:ClientId"];
        ClientSecret = _configuration["SpotifyCredentials:ClientSecret"];
        RedirectUri = new Uri(_configuration["SpotifyCredentials:RedirectUri"]);
    }

    public Uri GetLoginUri()
    {
        var loginRequest = new SpAPI.LoginRequest(
        RedirectUri,
        ClientId,
        SpAPI.LoginRequest.ResponseType.Code
        )
        {
            Scope = new[] { SpAPI.Scopes.PlaylistReadPrivate, SpAPI.Scopes.PlaylistReadCollaborative }
        };
        var uri = loginRequest.ToUri();
        return uri;
    }

    public async Task CreateSpotifyClient(string code)
    {
        var response = await new SpAPI.OAuthClient().RequestToken(
        new SpAPI.AuthorizationCodeTokenRequest(ClientId, ClientSecret, code, RedirectUri));

        var session = _httpContextAccessor.HttpContext.Session;
        session.SetString("access_token", response.AccessToken);

        SpotifyClient = new SpotifyClient(session.GetString("access_token"));

        /*
        var config = SpAPI.SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new SpAPI.AuthorizationCodeAuthenticator("ClientId", "ClientSecret", response));

        Spotify = new SpAPI.SpotifyClient(config); */
    }
}
