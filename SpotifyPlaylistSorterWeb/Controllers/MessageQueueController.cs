using Microsoft.AspNetCore.Mvc;
using SpotifyPlaylistSorter.Business.Services;
using SpotifyPlaylistSorterWeb.Filters;

namespace SpotifyPlaylistSorterWeb.Controllers
{
    public class MessageQueueController : Controller
    {
        private readonly IPlaylistsService _playlistService;

        public MessageQueueController(IPlaylistsService playlistService)
        {
            _playlistService = playlistService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSpotifyLogin]
        public async Task<IActionResult> AnalysePlaylist(string playlistId)
        {
            if (string.IsNullOrEmpty(playlistId))
            {
                return BadRequest(new { success = false, message = "Playlist ID is required." });
            }

            await _playlistService.AnalysePlaylist(playlistId);

            return Json(new { success = true, message = "Playlist queued for analysis." });
        }
    }
}
