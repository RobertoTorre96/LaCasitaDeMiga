using LaCasitaDeMiga.Features.Users.DTOs;

namespace LaCasitaDeMiga.Features.Users.services {
    public interface IUserService {
        Task<AuthResponseDto> GoogleLoginAsync(GoogleTokenRequestDto request);
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    }
}