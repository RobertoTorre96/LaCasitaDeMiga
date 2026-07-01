using AutoMapper;
using LaCasitaDeMiga.Features.Brands.DTOs;

namespace LaCasitaDeMiga.Features.Brands.Mapper {
    public class BrandMappingProfile :Profile {
        public BrandMappingProfile() {
            CreateMap<BrandEntity, BrandResponseDto>();
            CreateMap<BrandRequestDto, BrandEntity>();
        }
    }
}
