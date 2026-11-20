using AutoMapper;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Domain.Entities;

namespace BilisimPersoneli.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Departman, DepartmanDto>();
        CreateMap<DepartmanCreateDto, Departman>();
        CreateMap<DepartmanUpdateDto, Departman>();

        CreateMap<Personel, PersonelDto>()
            .ForMember(d => d.DepartmanAd, opt => opt.MapFrom(s => s.Departman != null ? s.Departman.Ad : null));
        CreateMap<PersonelCreateDto, Personel>()
            .ForMember(d => d.IdentityUserId, opt => opt.Ignore());
        CreateMap<PersonelUpdateDto, Personel>()
            .ForMember(d => d.IdentityUserId, opt => opt.Ignore());

        CreateMap<Izin, IzinDto>()
            .ForMember(d => d.PersonelAdSoyad, opt => opt.MapFrom(s =>
                s.Personel != null ? $"{s.Personel.Ad} {s.Personel.Soyad}" : null));
        CreateMap<IzinCreateDto, Izin>();
        CreateMap<IzinUpdateDto, Izin>();

        CreateMap<PersonelTest, PersonelTestDto>().ForMember(d => d.AdSoyad, opt => opt.MapFrom(s =>
            s.Personel != null ? $"{s.Personel.Ad} {s.Personel.Soyad}" : null));
        CreateMap<CreatePersonelTestDto, PersonelTest>();
        CreateMap<UpdatePersonelTestDto, PersonelTest>();

        CreateMap<AuditLog, AuditLogDto>();
    }
}
