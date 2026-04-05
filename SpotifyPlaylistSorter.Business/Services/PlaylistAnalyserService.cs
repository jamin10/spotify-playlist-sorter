using Microsoft.Extensions.Logging;
using SpotifyPlaylistSorter.Business.Dtos;
using SpotifyPlaylistSorter.Business.Mappers;
using SpotifyPlaylistSorter.Common.Models.QueueMessages;
using DomainModels = SpotifyPlaylistSorter.Domain.Models;

namespace SpotifyPlaylistSorter.Business.Services;

public class PlaylistAnalyserService(
    ICyaniteService _cyaniteService,
    ISpotifyService _spotifyService,
    ITrackStore _store,
    ILogger<PlaylistAnalyserService> _logger) : IAnalyserService
{

    public async Task<bool> Analyse(AnalysePlaylist message)
    {
        var allTracks = await LoadAndFetchTracksAsync(message);
        var playlist = await ResolvePlaylistAsync(message.PlaylistId);
        LinkTracksToPlaylist(allTracks, playlist);
        await _store.SaveChangesAsync();
        return true;
    }

    private async Task<List<DomainModels.Track>> LoadAndFetchTracksAsync(AnalysePlaylist message)
    {
        var existingTracks = await _store.GetExistingTracksAsync(message.TrackIds);
        var existingTrackIds = existingTracks.Select(t => t.SpotifyTrackId).ToHashSet();
        var trackIdsToAnalyse = message.TrackIds.Where(id => !existingTrackIds.Contains(id)).ToList();

        var newTracks = await FetchAndPersistNewTracksAsync(trackIdsToAnalyse);
        return [.. existingTracks, .. newTracks];
    }

    private async Task<List<DomainModels.Track>> FetchAndPersistNewTracksAsync(List<string> trackIds)
    {
        var artistCache = new Dictionary<string, DomainModels.Artist>();
        var albumCache = new Dictionary<string, DomainModels.Album>();
        var tracks = new List<DomainModels.Track>();

        foreach (var trackId in trackIds)
        {
            var dto = await FetchAnalysedTrackAsync(trackId);
            if (dto is null) continue;

            var album = await ResolveAlbumAsync(dto.Album, albumCache, artistCache);
            var artists = await ResolveArtistsAsync(dto.Artists, artistCache);
            var track = TrackEntityMapper.ToTrack(dto, album, artists);
            _store.Add(track);
            tracks.Add(track);
        }

        return tracks;
    }

    private async Task<AnalysedTrackDto?> FetchAnalysedTrackAsync(string trackId)
    {
        try
        {
            var spotifyTrack = await _spotifyService.GetTrackAsync(trackId);
            var cyaniteTrack = await _cyaniteService.GetTrackAnalysis(trackId);
            return new AnalysedTrackDto(spotifyTrack, cyaniteTrack);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyse track {TrackId}", trackId);
            return null;
        }
    }

    private async Task<DomainModels.Playlist> ResolvePlaylistAsync(string spotifyPlaylistId)
    {
        var playlist = await _store.FindPlaylistAsync(spotifyPlaylistId);
        if (playlist is not null)
            return playlist;

        var spotifyPlaylist = await _spotifyService.GetPlaylistAsync(spotifyPlaylistId);
        playlist = new DomainModels.Playlist
        {
            SpotifyPlaylistId = spotifyPlaylist.SpotifyPlaylistId,
            Name = spotifyPlaylist.Name,
            Description = spotifyPlaylist.Description,
            ImageUrl = spotifyPlaylist.ImageUrl
        };
        _store.Add(playlist);
        return playlist;
    }

    private static void LinkTracksToPlaylist(List<DomainModels.Track> tracks, DomainModels.Playlist playlist)
    {
        foreach (var track in tracks)
        {
            if (!track.Playlists.Contains(playlist))
                track.Playlists.Add(playlist);
        }
    }

    private async Task<List<DomainModels.Artist>> ResolveArtistsAsync(
        List<ArtistDto> artistDtos,
        Dictionary<string, DomainModels.Artist> artistCache)
    {
        var artists = new List<DomainModels.Artist>();
        foreach (var dto in artistDtos)
            artists.Add(await ResolveArtistAsync(dto.SpotifyArtistId, dto.Name, artistCache));
        return artists;
    }

    private async Task<DomainModels.Album> ResolveAlbumAsync(
        AlbumModelDto albumDto,
        Dictionary<string, DomainModels.Album> albumCache,
        Dictionary<string, DomainModels.Artist> artistCache)
    {
        if (albumCache.TryGetValue(albumDto.SpotifyId, out var cached))
            return cached;

        var album = await _store.FindAlbumAsync(albumDto.SpotifyId);
        if (album is null)
        {
            var artists = new List<DomainModels.Artist>();
            foreach (var artistDto in albumDto.Artists)
                artists.Add(await ResolveArtistAsync(artistDto.SpotifyArtistId, artistDto.Name, artistCache));

            album = new DomainModels.Album
            {
                SpotifyId = albumDto.SpotifyId,
                Name = albumDto.Name,
                Artists = artists
            };
            _store.Add(album);
        }

        albumCache[albumDto.SpotifyId] = album;
        return album;
    }

    private async Task<DomainModels.Artist> ResolveArtistAsync(
        string spotifyArtistId,
        string name,
        Dictionary<string, DomainModels.Artist> artistCache)
    {
        if (artistCache.TryGetValue(spotifyArtistId, out var cached))
            return cached;

        var artist = await _store.FindArtistAsync(spotifyArtistId)
            ?? new DomainModels.Artist { SpotifyArtistId = spotifyArtistId, Name = name };

        if (artist.Id == 0)
            _store.Add(artist);

        artistCache[spotifyArtistId] = artist;
        return artist;
    }
}
