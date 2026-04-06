using SpotifyAPI.Web;
using SpAPI = SpotifyAPI.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using SpotifyPlaylistSorter.Business.Dtos;
using SpotifyPlaylistSorter.Business.Services;

namespace SpotifyPlaylistSorter.Infrastructure.ExternalServices;

public class SpotifyService : ISpotifyService
{
    private readonly IConfiguration _configuration;

    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly string? ClientId;

    private readonly string? ClientSecret;

    private readonly Uri? RedirectUri;

    public SpotifyClient? SpotifyClient { get; set; }

    public SpotifyService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;

        ClientId = _configuration["SpotifyCredentials:ClientId"];
        ClientSecret = _configuration["SpotifyCredentials:ClientSecret"];
        RedirectUri = new Uri(_configuration["SpotifyCredentials:RedirectUri"]);
    }

    public Uri GetLoginUri()
    {
        var loginRequest = new SpAPI.LoginRequest(
        RedirectUri,
        ClientId,
        SpAPI.LoginRequest.ResponseType.Code
        )
        {
            Scope = new[] { SpAPI.Scopes.PlaylistReadPrivate, SpAPI.Scopes.PlaylistReadCollaborative }
        };
        var uri = loginRequest.ToUri();
        return uri;
    }

    public async Task CreateSpotifyClient(string code)
    {
        var response = await new SpAPI.OAuthClient().RequestToken(
        new SpAPI.AuthorizationCodeTokenRequest(ClientId, ClientSecret, code, RedirectUri));

        var session = _httpContextAccessor.HttpContext.Session;
        session.Set("access_token", System.Text.Encoding.UTF8.GetBytes(response.AccessToken));

        var val = session.TryGetValue("access_token", out var accessToken);
        SpotifyClient = new SpotifyClient(System.Text.Encoding.UTF8.GetString(accessToken));
    }

    public async Task AuthenticateWithClientCredentialsAsync()
    {
        var response = await new SpotifyAPI.Web.OAuthClient().RequestToken(
            new SpotifyAPI.Web.ClientCredentialsRequest(ClientId, ClientSecret)
        );
        SpotifyClient = new SpotifyAPI.Web.SpotifyClient(response.AccessToken);
    }

    public async Task<SpotifyTrackDto> GetTrackAsync(string trackId)
    {
        if (SpotifyClient is null)
            await AuthenticateWithClientCredentialsAsync();

        var track = await SpotifyClient!.Tracks.Get(trackId);
        return new SpotifyTrackDto(
            SpotifyTrackId: track.Id,
            Title: track.Name,
            Album: new AlbumModelDto
            {
                SpotifyId = track.Album.Id,
                Name = track.Album.Name,
                Artists = track.Album.Artists
                    .Select(a => new ArtistDto { SpotifyArtistId = a.Id ?? string.Empty, Name = a.Name })
                    .ToList()
            },
            Artists: track.Artists
                .Select(a => new ArtistDto { SpotifyArtistId = a.Id ?? string.Empty, Name = a.Name })
                .ToList()
        );
    }

    public async Task<SpotifyPlaylistDto> GetPlaylistAsync(string playlistId)
    {
        if (SpotifyClient is null)
            await AuthenticateWithClientCredentialsAsync();

        var playlist = await SpotifyClient!.Playlists.Get(playlistId);
        return new SpotifyPlaylistDto(
            SpotifyPlaylistId: playlistId,
            Name: playlist.Name ?? string.Empty,
            Description: playlist.Description,
            ImageUrl: playlist.Images?.FirstOrDefault()?.Url
        );
    }
}
