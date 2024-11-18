using SpotifyAPI.Web;

namespace SpotifyPlaylistSorterWeb.Models;

public class CurrentUserViewModel
{
    public bool IsLoggedIn { get; set; } = false;
    
    public PrivateUser? UserProfile { get; set; }
}