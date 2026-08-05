using AutoMapper;
using AutoMapper.QueryableExtensions;
using Google.Apis.Auth;
using LaCasitaDeMiga.Common.DTOs;
using LaCasitaDeMiga.Data;
using LaCasitaDeMiga.Exceptions;
using LaCasitaDeMiga.Features.Common.services.MailService;
using LaCasitaDeMiga.Features.Common.services.MailService.Enums;
using LaCasitaDeMiga.Features.Products.DTOs;
using LaCasitaDeMiga.Features.Users.DTOs;
using LaCasitaDeMiga.Features.Users.role;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LaCasitaDeMiga.Features.Users.services {
    public class UserServiceImpl : IUserService {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        //private readonly IEmailService _emailService; // 💡 Inyección agregada
        private readonly IEmailTemplateService _emailService; // 💡 Inyección agregada


        public UserServiceImpl(ApplicationDbContext context, IConfiguration configuration, IMapper mapper, IEmailTemplateService emailService) {
            _context = context;
            _configuration = configuration;
            _mapper = mapper;
            _emailService = emailService; 
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
                    Role = UserRole.Customer, // 💡 Cambiado a Enum
                    IsActive = true,
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
                PhoneNumber = request.PhoneNumber,
                PasswordHash = passwordHash,
                Role = UserRole.Customer, // 💡 Cambiado a Enum,
                IsActive = true,
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
            // 💡 Cortocircuito: Si el usuario fue desactivado por un Admin, no lo dejamos pasar
            if (!user.IsActive) {
                throw new BadRequestException("Tu cuenta se encuentra deshabilitada. Contacta al soporte.");
            }

            var userDto = _mapper.Map<UserResponseDto>(user);

            return new AuthResponseDto {
                User = userDto,
                Token = GenerateJwtToken(user)
            };
        }

        public async Task<PagedResultDto<UserResponseDto>> GetAllAsync(
                                                                    bool onlyActive = true,
                                                                    int pageNumber = 1,
                                                                    int pageSize = 10) {

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;


            var query = _context.Users.AsQueryable();

            if (onlyActive) query = query.Where(p => p.IsActive);

            var totalItems = await query.CountAsync();
          
            var users = await query
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<UserResponseDto>(_mapper.ConfigurationProvider)  
                .ToListAsync();



            return new PagedResultDto<UserResponseDto> {
                Items = users,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<bool> UpdateStatusAndRoleAsync(Guid id, UserUpdateRequestDto dto) {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.IsActive = dto.IsActive;
            user.Role = dto.Role; // Asignación directa Enum a Enum gracias al tipado fuerte

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto) {
            // 1. Buscamos al usuario que tenga el token ingresado
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == dto.Token);

            // 2. Si no existe o el token ya expiró (comparando con la hora UTC actual), tiramos excepción
            if (user == null || user.ResetTokenExpiry < DateTime.UtcNow) {
                throw new BadRequestException("El token de recuperación es inválido o ya ha expirado.");
            }

            // 3. Hasheamos la nueva contraseña de forma segura usando BCrypt
            string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.PasswordHash = newPasswordHash;

            // 4. IMPORTANTE: Borramos el token y su expiración para que no se pueda reutilizar el mismo enlace
            user.PasswordResetToken = null;
            user.ResetTokenExpiry = null;

            // 5. Guardamos los cambios en Neon
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }



        public async Task ForgotPasswordAsync(ForgotPasswordDto dto) {
            // 1. Buscamos si el usuario existe en Neon
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            // Por seguridad, si el usuario no existe, no le avisamos al front (evita que husmeen emails válidos)
            if (user == null) return;

            // 2. Generamos un token aleatorio único y seguro
            string token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

            // 3. Guardamos el token y el tiempo de expiración (15 minutos desde ahora)
            user.PasswordResetToken = token;
            user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // 4. Armamos el enlace que irá al Frontend (Localhost en desarrollo, tu web en producción)
            var frontendUrl = _configuration["Urls:Frontend"] ?? "http://localhost:5173";
            string resetLink = $"{frontendUrl}/reset-password?token={token}";

            var emailParams = new {
                USER_NAME = user.Name,
                RESET_LINK = resetLink
            };



            // 6. Despachamos el correo usando MailKit
            await _emailService.SendTemplateEmailAsync(user.Email, EEmailTemplate.FORGOT_EMAIL, emailParams);
        }

        public List<string> GetAvailableRoles() {
            // Obtiene todos los nombres definidos en el enum UserRole
            return Enum.GetNames(typeof(UserRole)).ToList();
        }


        // 🔐 NUEVO MÉTODO PRIVADO: EL MOTOR QUE GENERA EL JWT
        private string GenerateJwtToken(UserEntity user) {
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var durationStr = _configuration["Jwt:DurationInMinutes"] ?? "60";
            var expiry = DateTime.UtcNow.AddMinutes(double.Parse(durationStr));

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