using AutoMapper;
using AutoMapper.Execution;
using SpotifyAPI.Web;
using SpotifyPlaylistSorterWeb.Models;

namespace SpotifyPlaylistSorterWeb.Mappers;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<PrivateUser, CurrentUserViewModel>(MemberList.Source);

        CreateMap<Paging<FullPlaylist>, PaginatedList<FullPlaylistModel>>(MemberList.Source);

        //CreateMap<Paging<PlaylistTrack>, PaginatedList<TrackModel>>()

        CreateMap<FullPlaylist, FullPlaylistModel>(MemberList.Source)
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.TracksUrl, opt => opt.MapFrom(src => src.Href))
            .ForMember(dest => dest.SpotifyId, opt => opt.MapFrom(src => src.Id))
            .ForAllMembers(opts =>
            {
                opts.Condition((src, dest, srcMember) => srcMember != null);
            });

        CreateMap<Paging<PlaylistTrack<IPlayableItem>>, FullPlaylistModel>(MemberList.Source)
            .ForMember(dest => dest.TracksUrl, opt => opt.MapFrom(src => src.Href));
    }
}