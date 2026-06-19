using AutoMapper;
using ECommersAPI.Features.Brands.DTOs;

namespace ECommersAPI.Features.Brands.Mapper {
    public class BrandMappingProfile :Profile {
        public BrandMappingProfile() {
            CreateMap<BrandEntity, BrandResponseDto>();
            CreateMap<BrandRequestDto, BrandEntity>();
        }
    }
}
