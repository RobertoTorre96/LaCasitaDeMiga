using LaCasitaDeMiga.Features.Users.DTOs;
using LaCasitaDeMiga.Features.Users.services;
using Microsoft.AspNetCore.Mvc;

namespace LaCasitaDeMiga.Features.Users.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase {
        private readonly IUserService _userService;

        public AuthController(IUserService userService) {
            _userService = userService;
        }

        // 1. POST: api/Auth/google-login
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleTokenRequestDto request) {
            // Retorna un AuthResponseDto con los datos del usuario de Google y su JWT
            var result = await _userService.GoogleLoginAsync(request);
            return Ok(result);
        }

        // 2. POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request) {
            // Retorna un AuthResponseDto al registrarse con éxito para iniciar sesión al instante
            var result = await _userService.RegisterAsync(request);
            return Ok(result);
        }

        // 3. POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request) {
            // Retorna un AuthResponseDto si las credenciales tradicionales son correctas
            var result = await _userService.LoginAsync(request);
            return Ok(result);
        }
    }
}