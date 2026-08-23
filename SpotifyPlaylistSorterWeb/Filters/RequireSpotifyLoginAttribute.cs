using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using SpotifyPlaylistSorter.Business.Services;

namespace SpotifyPlaylistSorterWeb.Filters;

/// <summary>
/// Redirects to /Spotify/Login when there's no Spotify session, replacing the
/// SpotifyClient == null checks previously duplicated at each call site.
/// </summary>
public class RequireSpotifyLoginAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var sessionAuth = context.HttpContext.RequestServices.GetRequiredService<ISpotifySessionAuth>();
        if (!sessionAuth.IsAuthenticated)
        {
            context.Result = new RedirectResult("/Spotify/Login");
        }
    }
}
