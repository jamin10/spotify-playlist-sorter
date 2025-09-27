using SpotifyAPI.Web;

namespace SpotifyPlaylistSorter.Business.Models;

public class CurrentUserViewModel : PrivateUser
{
    public bool IsLoggedIn { get; set; }
}