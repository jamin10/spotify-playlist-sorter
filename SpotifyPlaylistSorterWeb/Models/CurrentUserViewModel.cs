using SpotifyAPI.Web;

namespace SpotifyPlaylistSorterWeb.Models;

public class CurrentUserViewModel : PrivateUser
{
    public bool IsLoggedIn { get; set; } = false;
}