using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SpotifyPlaylistSorterWeb.Models;
using SpotifyAPI.Web;

namespace SpotifyPlaylistSorterWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly ISpotifyService _spotify;

        public HomeController(ILogger<HomeController> logger, ISpotifyService spotifyClient)
        {
            _logger = logger;
            _spotify = spotifyClient;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Playlists()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
