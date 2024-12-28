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

    public async Task<IActionResult> Current(int page = 1, int pageSize = 10)
    {
        var viewModel = await _playlistsService.GetPlaylistsViewModel(page, pageSize);
        return View(viewModel);
    }

    public async Task<IActionResult> ViewPlaylist()
    {
        var viewModel = await _playlistsService.GetFullPlaylistViewModel();
        return View(viewModel);
    }
}