using System.Configuration;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using SpotifyAPI.Web;
using SpotifyPlaylistSorterWeb.Services.Interfaces;
using static System.Formats.Asn1.AsnWriter;

namespace SpotifyPlaylistSorterWeb.Controllers
{
    public class SpotifyController : Controller
    {
        private readonly ISpotifyService _spotifyService;

        public SpotifyController(ISpotifyService spotifyService)
        {
            _spotifyService = spotifyService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
        {
            var uri = _spotifyService.GetLoginUri();
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
            await _spotifyService.CreateSpotifyClient(code); 
            // Also important for later: response.RefreshToken
        }
    }
}
