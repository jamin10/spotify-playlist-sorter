using Microsoft.AspNetCore.Mvc;

namespace SpotifyPlaylistSorterWeb.Controllers
{
    public class MessageQueueController : Controller
    {
        public MessageQueueController()
        {
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AnalysePlaylist(string playlistId)
        {
            if (string.IsNullOrEmpty(playlistId))
            {
                return BadRequest(new { success = false, message = "Playlist ID is required." });
            }

            // TODO: Add logic to send the playlistId to your queue/message bus here.

            return Json(new { success = true, message = "Playlist queued for analysis." });
        }
    }
}