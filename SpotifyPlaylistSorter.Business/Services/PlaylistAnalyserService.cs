using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistSorter.Business.Dtos;
using SpotifyPlaylistSorter.Common.Models.QueueMessages;
using SpotifyPlaylistSorter.Domain;
using DomainModels = SpotifyPlaylistSorter.Domain.Models;

namespace SpotifyPlaylistSorter.Business.Services;

public class PlaylistAnalyserService : IAnalyserService
{
    private readonly ICyaniteService _cyaniteService;
    private readonly ISpotifyService _spotifyService;
    private readonly AppDbContext _dbContext;

    public PlaylistAnalyserService(ICyaniteService cyaniteService, ISpotifyService spotifyService, AppDbContext dbContext)
    {
        _cyaniteService = cyaniteService;
        _spotifyService = spotifyService;
        _spotifyService.AuthenticateWithClientCredentialsAsync();
        _dbContext = dbContext;
    }

    public async Task<bool> Analyse(AnalysePlaylist message)
    {
        var analysedTracks = new List<AnalysedTrackDto>();

        foreach (var trackId in message.TrackIds)
        {
            var cyaniteTrack = await _cyaniteService.GetTrackAnalysis(trackId);
            var spotifyTrack = await _spotifyService.SpotifyClient.Tracks.Get(trackId);
            analysedTracks.Add(new AnalysedTrackDto(spotifyTrack, cyaniteTrack));
        }

        var spotifyPlaylist = await _spotifyService.SpotifyClient.Playlists.Get(message.PlaylistId);

        var playlist = await _dbContext.Playlists
            .Include(p => p.Tracks)
            .FirstOrDefaultAsync(p => p.SpotifyPlaylistId == message.PlaylistId);

        if (playlist is null)
        {
            playlist = new DomainModels.Playlist
            {
                SpotifyPlaylistId = message.PlaylistId,
                Name = spotifyPlaylist.Name ?? string.Empty,
                Description = spotifyPlaylist.Description,
                ImageUrl = spotifyPlaylist.Images?.FirstOrDefault()?.Url
            };
            _dbContext.Playlists.Add(playlist);
        }

        foreach (var dto in analysedTracks)
        {
            var track = await _dbContext.Tracks
                .Include(t => t.Artists)
                .Include(t => t.Playlists)
                .FirstOrDefaultAsync(t => t.SpotifyTrackId == dto.SpotifyTrackId);

            if (track is null)
            {
                var album = await ResolveAlbumAsync(dto.Album);

                var artists = new List<DomainModels.Artist>();
                foreach (var artistDto in dto.Artists)
                    artists.Add(await ResolveArtistAsync(artistDto.SpotifyArtistId, artistDto.Name));

                track = new DomainModels.Track
                {
                    SpotifyTrackId = dto.SpotifyTrackId,
                    Title = dto.Title,
                    Album = album,
                    Artists = artists,
                    AudioFeatures = new DomainModels.TrackAudioFeatures
                    {
                        EnergyLevel = dto.AudioFeatures.EnergyLevel,
                        EnergyDynamics = dto.AudioFeatures.EnergyDynamics,
                        Bpm = dto.AudioFeatures.Bpm,
                        BpmRangeAdjusted = dto.AudioFeatures.BpmRangeAdjusted
                    }
                };
                _dbContext.Tracks.Add(track);
            }

            if (!track.Playlists.Contains(playlist))
                track.Playlists.Add(playlist);
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    private async Task<DomainModels.Album> ResolveAlbumAsync(AlbumModelDto albumDto)
    {
        var album = await _dbContext.Albums
            .Include(a => a.Artists)
            .FirstOrDefaultAsync(a => a.SpotifyId == albumDto.SpotifyId);

        if (album is not null)
            return album;

        var artists = new List<DomainModels.Artist>();
        foreach (var artistDto in albumDto.Artists)
            artists.Add(await ResolveArtistAsync(artistDto.SpotifyArtistId, artistDto.Name));

        album = new DomainModels.Album
        {
            SpotifyId = albumDto.SpotifyId,
            Name = albumDto.Name,
            Artists = artists
        };
        _dbContext.Albums.Add(album);
        return album;
    }

    private async Task<DomainModels.Artist> ResolveArtistAsync(string spotifyArtistId, string name)
    {
        var artist = await _dbContext.Artists
            .FirstOrDefaultAsync(a => a.SpotifyArtistId == spotifyArtistId);

        if (artist is not null)
            return artist;

        artist = new DomainModels.Artist
        {
            SpotifyArtistId = spotifyArtistId,
            Name = name
        };
        _dbContext.Artists.Add(artist);
        return artist;
    }
}
