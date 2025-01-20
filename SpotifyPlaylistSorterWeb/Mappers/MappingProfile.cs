using AutoMapper;
using AutoMapper.Execution;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using SpotifyAPI.Web;
using SpotifyPlaylistSorterWeb.Models;

namespace SpotifyPlaylistSorterWeb.Mappers;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap(typeof(Paging<>), typeof(PaginatedList<>));

        CreateMap<PrivateUser, CurrentUserViewModel>(MemberList.Source);

        CreateMap<FullTrack, TrackModel>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));

        CreateMap<IPlayableItem, TrackModel>()
            .Include<FullTrack, TrackModel>();

        CreateMap<SimpleArtist, ArtistModel>()
            .ForMember(dest => dest.SpotifyArtistId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));

        CreateMap<PlaylistTrack<IPlayableItem>, TrackModel>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => ((FullTrack)src.Track).Name))
            .ForMember(dest => dest.SpotifyTrackId, opt => opt.MapFrom(src => ((FullTrack)src.Track).Id))
            .ForMember(dest => dest.Artists, opt => opt.MapFrom(src => ((FullTrack)src.Track).Artists));

        CreateMap<FullPlaylist, FullPlaylistModel>(MemberList.Source)
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.TracksUrl, opt => opt.MapFrom(src => src.Href))
            .ForMember(dest => dest.SpotifyId, opt => opt.MapFrom(src => src.Id))
            .ForAllMembers(opts =>
            {
                opts.Condition((src, dest, srcMember) => srcMember != null);
            });

        //CreateMap<Paging<PlaylistTrack<IPlayableItem>>, FullPlaylistModel>(MemberList.Source)
        //    .ForMember(dest => dest.TracksUrl, opt => opt.MapFrom(src => src.Href));
    }
}