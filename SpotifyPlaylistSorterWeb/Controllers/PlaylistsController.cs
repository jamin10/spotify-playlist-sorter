using Microsoft.AspNetCore.Mvc;
using SpotifyPlaylistSorterWeb.Clients.Interfaces;
using SpotifyPlaylistSorterWeb.Services.Interfaces;

namespace SpotifyPlaylistSorterWeb.Controllers;

public class PlaylistsController : Controller
{
    private readonly ILogger<PlaylistsController> _logger;

    private readonly IPlaylistsService _playlistsService;

    private readonly ICyaniteClient _cyaniteClient;

    public PlaylistsController(ILogger<PlaylistsController> logger, IPlaylistsService playlistsService, ICyaniteClient cyaniteClient)
    {
        _logger = logger;
        _playlistsService = playlistsService;
        _cyaniteClient = cyaniteClient;
    }

    public async Task<IActionResult> Current()
    {
        var viewModel = await _playlistsService.GetPlaylistsViewModel();
        var query2 = "query SpotifyTrackQuery($spotifyTrackId: ID!) { spotifyTrack(id: $spotifyTrackId) { __typename ... on SpotifyTrackError { message } ... on SpotifyTrack { id title audioAnalysisV6 { __typename ... on AudioAnalysisV6Finished { result { energyLevel energyDynamics bpmPrediction { value confidence } bpmRangeAdjusted } } } } } }";
        var variables2 = new { spotifyTrackId = "6suVCaWE1ssKwdnLJyjyxy" };

        var result = await _cyaniteClient.GetAsync(query2, variables2);
        return View(viewModel);
    }
}