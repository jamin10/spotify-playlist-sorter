using AutoMapper;
using SpotifyAPI.Web;
using SpotifyPlaylistSorter.Business.Models;

namespace SpotifyPlaylistSorter.Business.Services;

public class SpotifyUserContext : ISpotifyUserContext
{
    private readonly ISpotifyCredentialProvider _credentialProvider;
    private readonly IMapper _mapper;

    public SpotifyUserContext(ISpotifyCredentialProvider credentialProvider, IMapper mapper)
    {
        _credentialProvider = credentialProvider;
        _mapper = mapper;
    }

    public async Task<PaginatedList<FullPlaylistModel>> GetCurrentUserPlaylistsAsync(int page, int pageSize)
    {
        var client = await _credentialProvider.GetClientAsync();
        var request = new PlaylistCurrentUsersRequest
        {
            Limit = pageSize,
            Offset = (page - 1) * pageSize
        };

        var playlists = await client.Playlists.CurrentUsers(request);

        var result = new PaginatedList<FullPlaylistModel> { Index = page, Limit = pageSize };
        if (playlists != null)
        {
            _mapper.Map(playlists, result);
        }
        return result;
    }

    public async Task<PaginatedList<TrackModel>> GetPlaylistItemsAsync(string playlistId, int page, int pageSize)
    {
        var client = await _credentialProvider.GetClientAsync();
        var request = new PlaylistGetItemsRequest
        {
            Limit = pageSize,
            Offset = (page - 1) * pageSize,
        };

        var playlist = await client.Playlists.GetItems(playlistId, request);

        var result = new PaginatedList<TrackModel> { Index = page, Limit = pageSize };
        if (playlist != null)
        {
            _mapper.Map(playlist, result);
        }
        return result;
    }

    public async Task<List<string>> GetAllTrackIdsAsync(string playlistId)
    {
        var client = await _credentialProvider.GetClientAsync();
        var allTrackIds = new List<string>();
        var request = new PlaylistGetItemsRequest { Limit = 100, Offset = 0 };
        Paging<PlaylistTrack<IPlayableItem>> tracks;

        do
        {
            tracks = await client.Playlists.GetItems(playlistId, request);
            if (tracks?.Items == null) break;

            allTrackIds.AddRange(
                tracks.Items
                    .Select(t => t.Track as FullTrack)
                    .Where(ft => ft != null && ft.Id != null)
                    .Select(ft => ft!.Id!)
            );

            request.Offset += request.Limit;
        }
        while (tracks.Next != null);

        return allTrackIds;
    }

    public async Task<PrivateUser> GetCurrentUserAsync()
    {
        var client = await _credentialProvider.GetClientAsync();
        return await client.UserProfile.Current();
    }
}
