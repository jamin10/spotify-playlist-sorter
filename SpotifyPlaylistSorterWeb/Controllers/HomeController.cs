using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SpotifyPlaylistSorterWeb.Models;
using SpotifyPlaylistSorterWeb.Services.Interfaces;

namespace SpotifyPlaylistSorterWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly IUserProfileService _userProfileService;

        public HomeController(ILogger<HomeController> logger, IUserProfileService userProfileService)
        {
            _logger = logger;
            _userProfileService = userProfileService;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = await _userProfileService.GetCurrentUserViewModel();
            return View(viewModel);
        }

        public async Task<IActionResult> Playlists()
        {
            //var playlists = await _spotifyService.SpotifyClient.Playlists.CurrentUsers();
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
