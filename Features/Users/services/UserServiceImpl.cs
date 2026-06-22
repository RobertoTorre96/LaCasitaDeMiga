using Google.Apis.Auth;
using LaCasitaDeMiga.Data;
using LaCasitaDeMiga.Exceptions;
using LaCasitaDeMiga.Features.Users.DTOs;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace LaCasitaDeMiga.Features.Users.services {
    public class UserServiceImpl : IUserService {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public UserServiceImpl(ApplicationDbContext context, IConfiguration configuration, IMapper mapper) {
            _context = context;
            _configuration = configuration;
            _mapper = mapper;
        }

        // 1. INICIO DE SESIÓN CON GOOGLE (Modificado el retorno)
        public async Task<AuthResponseDto> GoogleLoginAsync(GoogleTokenRequestDto request) {
            var clientId = _configuration["Authentication:Google:ClientId"];
            var settings = new GoogleJsonWebSignature.ValidationSettings() { Audience = new[] { clientId } };

            var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

            if (user == null) {
                user = new UserEntity {
                    Id = Guid.NewGuid(),
                    Email = payload.Email,
                    Name = payload.Name,
                    PictureUrl = payload.Picture,
                    Role = "Customer",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // Mapeamos al DTO seguro
            var userDto = _mapper.Map<UserResponseDto>(user);

            // Metemos todo dentro de la "caja contenedor" AuthResponseDto junto al Token
            return new AuthResponseDto {
                User = userDto,
                Token = GenerateJwtToken(user)
            };
        }

        // 2. REGISTRO TRADICIONAL (Modificado el retorno)
        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request) {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null) {
                throw new BadRequestException("El correo electrónico ya está registrado.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new UserEntity {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Name = request.Name,
                PasswordHash = passwordHash,
                Role = "Customer",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var userDto = _mapper.Map<UserResponseDto>(newUser);

            return new AuthResponseDto {
                User = userDto,
                Token = GenerateJwtToken(newUser)
            };
        }

        // 3. LOGIN TRADICIONAL (Modificado el retorno)
        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request) {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash)) {
                throw new UnauthorizedException("Credenciales incorrectas.");
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid) {
                throw new UnauthorizedException("Credenciales incorrectas.");
            }

            var userDto = _mapper.Map<UserResponseDto>(user);

            return new AuthResponseDto {
                User = userDto,
                Token = GenerateJwtToken(user)
            };
        }

        // 🔐 NUEVO MÉTODO PRIVADO: EL MOTOR QUE GENERA EL JWT
        private string GenerateJwtToken(UserEntity user) {
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:DurationInMinutes"]!));

            var token = new SecurityTokenDescriptor {
                Subject = new ClaimsIdentity(claims),
                Expires = expiry,
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(token);

            return tokenHandler.WriteToken(securityToken);
        }
    }
}