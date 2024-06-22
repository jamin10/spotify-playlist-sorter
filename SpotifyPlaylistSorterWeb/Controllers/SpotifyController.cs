using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using SpotifyAPI.Web;
using static System.Formats.Asn1.AsnWriter;
using SpAPI = SpotifyAPI.Web;

namespace SpotifyPlaylistSorterWeb.Controllers
{
    public class SpotifyController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
        {
            string ClientId = "be0324574d3d4350aaa1763a6b25e4d9";

            var loginRequest = new SpAPI.LoginRequest(
            new Uri("https://localhost:44314/Spotify/Callback"),
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
              new AuthorizationCodeTokenRequest("ClientId", "ClientSecret", code, new Uri("http://localhost:44314"))
            );

            var spotify = new SpotifyClient(response.AccessToken);
            // Also important for later: response.RefreshToken
        }
    }
}
