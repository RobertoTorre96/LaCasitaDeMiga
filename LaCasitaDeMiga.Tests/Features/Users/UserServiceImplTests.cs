using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using LaCasitaDeMiga.Data;
using LaCasitaDeMiga.Exceptions;
using LaCasitaDeMiga.Features.Users;
using LaCasitaDeMiga.Features.Users.DTOs;
using LaCasitaDeMiga.Features.Users.services;
using LaCasitaDeMiga.Features.Users.role;
using LaCasitaDeMiga.Features.Common.services.MailService;
using LaCasitaDeMiga.Features.Common.services.MailService.Enums;

namespace LaCasitaDeMiga.Tests.Features.Users.Services {
    public class UserServiceImplTests {
        // =========================================================================
        // ⚙️ CONFIGURACIONES Y AYUDANTES (Mappers, Configs)
        // =========================================================================
        private IConfiguration CreateMockConfiguration() {
            var inMemorySettings = new Dictionary<string, string> {
                {"Jwt:Key", "SUPER_SECRET_KEY_THAT_HAS_TO_BE_VERY_LONG_12345!!"},
                {"Jwt:DurationInMinutes", "60"},
                {"Jwt:Issuer", "LaCasitaDeMiga"},
                {"Jwt:Audience", "LaCasitaDeMigaUsers"}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();
        }

        private IMapper CreateRealMapper() {
            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<UserEntity, UserResponseDto>();
            });
            return config.CreateMapper();
        }

        // =========================================================================
        // 🔐 PRUEBAS: LoginAsync
        // =========================================================================

        [Fact]
        public async Task LoginAsync_WhenUserDoesNotExist_ShouldThrowUnauthorizedException() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Login_UserDoesNotExist")
                .Options;

            using var context = new ApplicationDbContext(options);
            var mockConfig = new Mock<IConfiguration>();
            var mockMapper = new Mock<IMapper>();
            var mockMail = new Mock<IEmailTemplateService>();

            var service = new UserServiceImpl(context, mockConfig.Object, mockMapper.Object, mockMail.Object);

            var request = new LoginRequestDto { Email = "noexiste@correo.com", Password = "Password123" };

            var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(request));
            Assert.Equal("Credenciales incorrectas.", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_WhenPasswordIsIncorrect_ShouldThrowUnauthorizedException() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Login_PasswordIncorrect")
                .Options;

            using var context = new ApplicationDbContext(options);
            context.Users.Add(new UserEntity {
                Email = "roberto@miga.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("PasswordCorrecto123")
            });
            await context.SaveChangesAsync();

            var mockConfig = new Mock<IConfiguration>();
            var mockMapper = new Mock<IMapper>();
            var mockMail = new Mock<IEmailTemplateService>();

            var service = new UserServiceImpl(context, mockConfig.Object, mockMapper.Object, mockMail.Object);

            var request = new LoginRequestDto { Email = "roberto@miga.com", Password = "PasswordIncorrecto" };

            var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(request));
            Assert.Equal("Credenciales incorrectas.", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_WhenUserIsInactive_ShouldThrowBadRequestException() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Login_UserInactive")
                .Options;

            using var context = new ApplicationDbContext(options);
            context.Users.Add(new UserEntity {
                Email = "bloqueado@miga.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                IsActive = false 
            });
            await context.SaveChangesAsync();

            var mockConfig = new Mock<IConfiguration>();
            var mockMapper = new Mock<IMapper>();
            var mockMail = new Mock<IEmailTemplateService>();

            var service = new UserServiceImpl(context, mockConfig.Object, mockMapper.Object, mockMail.Object);

            var request = new LoginRequestDto { Email = "bloqueado@miga.com", Password = "Password123" };

            var exception = await Assert.ThrowsAsync<BadRequestException>(() => service.LoginAsync(request));
            Assert.Equal("Tu cuenta se encuentra deshabilitada. Contacta al soporte.", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_WhenCredentialsAreValid_ShouldReturnAuthResponseWithToken() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Login_Success")
                .Options;

            using var context = new ApplicationDbContext(options);
            context.Users.Add(new UserEntity {
                Email = "exito@miga.com",
                Name = "Roberto Torres",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("PasswordCorrecto123"),
                IsActive = true
            });
            await context.SaveChangesAsync();

            var service = new UserServiceImpl(context, CreateMockConfiguration(), CreateRealMapper(), new Mock<IEmailTemplateService>().Object);

            var request = new LoginRequestDto { Email = "exito@miga.com", Password = "PasswordCorrecto123" };

            var result = await service.LoginAsync(request);

            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.Equal("Roberto Torres", result.User.Name);
        }

        // =========================================================================
        // 📝 PRUEBAS: RegisterAsync
        // =========================================================================

        [Fact]
        public async Task RegisterAsync_WhenEmailAlreadyExists_ShouldThrowBadRequestException() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Register_EmailExists")
                .Options;

            using var context = new ApplicationDbContext(options);
            context.Users.Add(new UserEntity { Email = "repetido@miga.com" });
            await context.SaveChangesAsync();

            var service = new UserServiceImpl(context, new Mock<IConfiguration>().Object, new Mock<IMapper>().Object, new Mock<IEmailTemplateService>().Object);

            var request = new RegisterRequestDto { Email = "repetido@miga.com", Password = "Password123", Name = "Test" };

            var exception = await Assert.ThrowsAsync<BadRequestException>(() => service.RegisterAsync(request));
            Assert.Equal("El correo electrónico ya está registrado.", exception.Message);
        }

        [Fact]
        public async Task RegisterAsync_WhenDataIsValid_ShouldCreateUserAndReturnTokens() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Register_Success")
                .Options;

            using var context = new ApplicationDbContext(options);
            var service = new UserServiceImpl(context, CreateMockConfiguration(), CreateRealMapper(), new Mock<IEmailTemplateService>().Object);

            var request = new RegisterRequestDto { Email = "nuevo@miga.com", Password = "Password123", Name = "Nuevo Usuario", PhoneNumber = "123456" };

            var result = await service.RegisterAsync(request);

            Assert.NotNull(result);
            Assert.Equal("Nuevo Usuario", result.User.Name);

            // Verificamos que realmente se guardó en la base de datos en memoria
            var userInDb = await context.Users.FirstOrDefaultAsync(u => u.Email == "nuevo@miga.com");
            Assert.NotNull(userInDb);
        }

        // =========================================================================
        // 📊 PRUEBAS: GetAllAsync
        // =========================================================================

        [Fact]
        public async Task GetAllAsync_WhenParametersAreInvalid_ShouldApplySanitizationRules() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "GetAll_Sanitization")
                .Options;

            using var context = new ApplicationDbContext(options);
            context.Users.Add(new UserEntity { Email = "u1@miga.com", IsActive = true });
            await context.SaveChangesAsync();

            var service = new UserServiceImpl(context, new Mock<IConfiguration>().Object, CreateRealMapper(), new Mock<IEmailTemplateService>().Object);

            // Pasamos pageNumber = 0 (menor a 1) y pageSize = 100 (mayor a 50)
            var result = await service.GetAllAsync(onlyActive: true, pageNumber: 0, pageSize: 100);

            Assert.Equal(1, result.PageNumber);  // if (pageNumber < 1) pageNumber = 1;
            Assert.Equal(50, result.PageSize);   // if (pageSize > 50) pageSize = 50;
        }

        // =========================================================================
        // 🔄 PRUEBAS: UpdateStatusAndRoleAsync
        // =========================================================================

        [Fact]
        public async Task UpdateStatusAndRoleAsync_WhenUserDoesNotExist_ShouldReturnFalse() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Update_UserNotFound")
                .Options;

            using var context = new ApplicationDbContext(options);
            var service = new UserServiceImpl(context, new Mock<IConfiguration>().Object, new Mock<IMapper>().Object, new Mock<IEmailTemplateService>().Object);

            var dto = new UserUpdateRequestDto { IsActive = false, Role = UserRole.Admin };
            var result = await service.UpdateStatusAndRoleAsync(Guid.NewGuid(), dto);

            Assert.False(result); // Debe devolver false porque el Guid no existe en el DB
        }

        [Fact]
        public async Task UpdateStatusAndRoleAsync_WhenUserExists_ShouldUpdateFieldsAndReturnTrue() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Update_Success")
                .Options;

            var userId = Guid.NewGuid();

            using var context = new ApplicationDbContext(options);
            context.Users.Add(new UserEntity { Id = userId, IsActive = true, Role = UserRole.Customer });
            await context.SaveChangesAsync();

            var service = new UserServiceImpl(context, new Mock<IConfiguration>().Object, new Mock<IMapper>().Object, new Mock<IEmailTemplateService>().Object);

            var dto = new UserUpdateRequestDto { IsActive = false, Role = UserRole.Admin };
            var result = await service.UpdateStatusAndRoleAsync(userId, dto);

            Assert.True(result);

            // Comprobamos los cambios aplicados en la BD
            var updatedUser = await context.Users.FindAsync(userId);
            Assert.False(updatedUser!.IsActive);
            Assert.Equal(UserRole.Admin, updatedUser.Role);
        }

        // =========================================================================
        // 🔑 PRUEBAS: ResetPasswordAsync
        // =========================================================================

        [Fact]
        public async Task ResetPasswordAsync_WhenTokenIsExpiredOrInvalid_ShouldThrowBadRequestException() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Reset_TokenExpired")
                .Options;

            using var context = new ApplicationDbContext(options);
            context.Users.Add(new UserEntity {
                Email = "token.viejo@miga.com",
                PasswordResetToken = "TOKEN_EXPIRADO",
                ResetTokenExpiry = DateTime.UtcNow.AddMinutes(-5) // Expiró hace 5 minutos 💡
            });
            await context.SaveChangesAsync();

            var service = new UserServiceImpl(context, new Mock<IConfiguration>().Object, new Mock<IMapper>().Object, new Mock<IEmailTemplateService>().Object);

            var dto = new ResetPasswordDto { Token = "TOKEN_EXPIRADO", NewPassword = "NuevaPassword123" };

            var exception = await Assert.ThrowsAsync<BadRequestException>(() => service.ResetPasswordAsync(dto));
            Assert.Equal("El token de recuperación es inválido o ya ha expirado.", exception.Message);
        }
    }
}