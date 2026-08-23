using Microsoft.AspNetCore.Mvc;
using SpotifyPlaylistSorter.Business.Services;

namespace SpotifyPlaylistSorterWeb.Controllers
{
    public class SpotifyController : Controller
    {
        private readonly ISpotifySessionAuth _sessionAuth;

        public SpotifyController(ISpotifySessionAuth sessionAuth)
        {
            _sessionAuth = sessionAuth;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
        {
            return Redirect(_sessionAuth.GetLoginUri().ToString());
        }

        public async Task<IActionResult> Callback(string code)
        {
            await _sessionAuth.SignInAsync(code);
            return Redirect("/");
        }

        public IActionResult Logout()
        {
            _sessionAuth.SignOut();
            return Redirect("/");
        }
    }
}
