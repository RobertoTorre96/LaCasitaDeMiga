using LaCasitaDeMiga.Features.Users.DTOs;

namespace LaCasitaDeMiga.Features.Users.services {
    public interface IUserService {
        Task<AuthResponseDto> GoogleLoginAsync(GoogleTokenRequestDto request);
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);

        Task<bool> UpdateStatusAndRoleAsync(Guid id, UserUpdateRequestDto dto);
        List<string> GetAvailableRoles();
        Task ForgotPasswordAsync(ForgotPasswordDto dto);
        Task ResetPasswordAsync(ResetPasswordDto dto);
    }
}