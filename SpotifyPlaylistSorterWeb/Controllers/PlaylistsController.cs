using Microsoft.AspNetCore.Mvc;
using SpotifyPlaylistSorterWeb.Services.Interfaces;

namespace SpotifyPlaylistSorterWeb.Controllers;

public class PlaylistsController : Controller
{
    private readonly ILogger<PlaylistsController> _logger;

    private readonly IPlaylistsService _playlistsService;

    public PlaylistsController(ILogger<PlaylistsController> logger, IPlaylistsService playlistsService)
    {
        _logger = logger;
        _playlistsService = playlistsService;
    }

    public async Task<IActionResult> Get()
    {
        var viewModel = _playlistsService.GetPlaylistsViewModel();
        return View("Playlists");
    }
}