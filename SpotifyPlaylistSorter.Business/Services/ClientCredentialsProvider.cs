using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web;

namespace SpotifyPlaylistSorter.Business.Services;

/// <summary>
/// App-only credential adapter used by the Worker. Registered as a singleton: the client is
/// built once and reused, and ClientCredentialsAuthenticator refreshes the token itself before
/// it expires, so no manual expiry tracking is needed here.
/// </summary>
public class ClientCredentialsProvider : ISpotifyCredentialProvider
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private SpotifyClient? _client;

    public ClientCredentialsProvider(IConfiguration configuration)
    {
        _clientId = configuration["SpotifyCredentials:ClientId"]
            ?? throw new InvalidOperationException("Missing SpotifyCredentials:ClientId");
        _clientSecret = configuration["SpotifyCredentials:ClientSecret"]
            ?? throw new InvalidOperationException("Missing SpotifyCredentials:ClientSecret");
    }

    public Task<SpotifyClient> GetClientAsync()
    {
        _client ??= new SpotifyClient(SpotifyClientConfig.CreateDefault()
            .WithAuthenticator(new ClientCredentialsAuthenticator(_clientId, _clientSecret)));

        return Task.FromResult(_client);
    }
}
