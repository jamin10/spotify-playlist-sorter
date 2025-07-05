using Microsoft.AspNetCore.Mvc;
using SpotifyPlaylistSorterWeb.Services.Interfaces;

namespace SpotifyPlaylistSorterWeb.Controllers
{
    public class MessageQueueController : Controller
    {
        private readonly IMessageQueueService _messageQueueService;

        public MessageQueueController(IMessageQueueService messageQueueService)
        {
            _messageQueueService = messageQueueService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AnalysePlaylist(string playlistId)
        {
            if (string.IsNullOrEmpty(playlistId))
            {
                return BadRequest(new { success = false, message = "Playlist ID is required." });
            }

            _messageQueueService.SendMessage(playlistId);
            
            return Json(new { success = true, message = "Playlist queued for analysis." });
        }
    }
}