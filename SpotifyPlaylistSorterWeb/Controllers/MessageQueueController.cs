using Microsoft.AspNetCore.Mvc;
using SpotifyPlaylistSorterWeb.Services.Interfaces;

namespace SpotifyPlaylistSorterWeb.Controllers
{
    public class MessageQueueController : Controller
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly IPlaylistsService _playlistService;

        public MessageQueueController(
            IMessageQueueService messageQueueService,
            IPlaylistsService playlistService)
        {
            _messageQueueService = messageQueueService;
            _playlistService = playlistService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AnalysePlaylist(string playlistId)
        {
            if (string.IsNullOrEmpty(playlistId))
            {
                return BadRequest(new { success = false, message = "Playlist ID is required." });
            }

            var result = _playlistService.AnalysePlaylist(playlistId);
            //_messageQueueService.SendMessage(playlistId);
            
            return Json(new { success = true, message = "Playlist queued for analysis." });
        }
    }
}