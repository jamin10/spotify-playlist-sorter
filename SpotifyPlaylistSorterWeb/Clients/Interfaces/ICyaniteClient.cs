namespace SpotifyPlaylistSorterWeb.Clients.Interfaces;

public interface ICyaniteClient
{
    public Task<string> GetAsync(string query);
}