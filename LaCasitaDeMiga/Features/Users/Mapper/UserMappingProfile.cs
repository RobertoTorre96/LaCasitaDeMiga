using AutoMapper;
using LaCasitaDeMiga.Features.Users.DTOs;

namespace LaCasitaDeMiga.Features.Users.Mapper {
    public class UserMappingProfile :Profile{
        public UserMappingProfile() {
            CreateMap<UserEntity, UserResponseDto>();
        }

    }
}
