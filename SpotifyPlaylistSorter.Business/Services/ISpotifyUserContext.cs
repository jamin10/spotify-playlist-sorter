using SpotifyAPI.Web;
using SpotifyPlaylistSorter.Business.Models;

namespace SpotifyPlaylistSorter.Business.Services;

/// <summary>
/// Current-user-scoped Spotify data, used only by the Web app. Split from ISpotifyService
/// because the Worker (client-credentials, no user context) can never call these.
/// </summary>
public interface ISpotifyUserContext
{
    Task<PaginatedList<FullPlaylistModel>> GetCurrentUserPlaylistsAsync(int page, int pageSize);

    Task<PaginatedList<TrackModel>> GetPlaylistItemsAsync(string playlistId, int page, int pageSize);

    Task<List<string>> GetAllTrackIdsAsync(string playlistId);

    /// <summary>
    /// Returns PrivateUser directly rather than a DTO: CurrentUserViewModel currently inherits
    /// PrivateUser, so a DTO here would need its own Razor view rewrite - out of scope for the
    /// session-seam redesign.
    /// </summary>
    Task<PrivateUser> GetCurrentUserAsync();
}
