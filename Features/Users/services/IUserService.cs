using LaCasitaDeMiga.Common.DTOs;
using LaCasitaDeMiga.Features.Products.DTOs;
using LaCasitaDeMiga.Features.Users.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace LaCasitaDeMiga.Features.Users.services {
    public interface IUserService {
        Task<AuthResponseDto> GoogleLoginAsync(GoogleTokenRequestDto request);
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);

        Task<bool> UpdateStatusAndRoleAsync(Guid id, UserUpdateRequestDto dto);
        Task<PagedResultDto<UserResponseDto>> GetAllAsync(bool onlyActive = true,
                                                             int pageNumber = 1,
                                                             int pageSize = 10);
        List<string> GetAvailableRoles();
        Task ForgotPasswordAsync(ForgotPasswordDto dto);
        Task ResetPasswordAsync(ResetPasswordDto dto);
    }
}