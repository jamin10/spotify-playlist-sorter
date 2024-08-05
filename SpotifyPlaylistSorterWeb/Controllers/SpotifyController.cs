using System.Configuration;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using SpotifyAPI.Web;
using static System.Formats.Asn1.AsnWriter;
using SpAPI = SpotifyAPI.Web;

namespace SpotifyPlaylistSorterWeb.Controllers
{
    public class SpotifyController : Controller
    {
        private readonly IConfiguration _configuration;
        
        private readonly string? ClientId;

        private readonly string? ClientSecret;

        private readonly Uri? RedirectUri;

        public SpotifyController(IConfiguration configuration)
        {
            _configuration = configuration;
            ClientId = _configuration["SpotifyCredentials:ClientId"];
            ClientSecret = _configuration["SpotifyCredentials:ClientSecret"];
            RedirectUri = new Uri(_configuration["SpotifyCredentials:RedirectUri"]);
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
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
            return Redirect(uri.ToString());
        }

        public async Task<IActionResult> Callback()
        {
            string code = Request.Query["code"];
            await GetCallback(code);
            return Redirect("/");
        }
        public async Task GetCallback(string code)
        {
            var response = await new OAuthClient().RequestToken(
              new AuthorizationCodeTokenRequest(ClientId, ClientSecret, code, RedirectUri));

            var config = SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(new AuthorizationCodeAuthenticator("ClientId", "ClientSecret", response));

            var spotify = new SpotifyClient(config);
            // Also important for later: response.RefreshToken

            var track = await spotify.Tracks.Get("11dFghVXANMlKmJXsNCbNl");
            Console.WriteLine(track.Name);
        }
    }
}
