using Xunit;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using LaCasitaDeMiga.Data;
using LaCasitaDeMiga.Exceptions;
using LaCasitaDeMiga.Features.Brands;
using LaCasitaDeMiga.Features.Brands.DTOs;
using LaCasitaDeMiga.Features.Brands.Services;
using LaCasitaDeMiga.Features.Brands.Mapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LaCasitaDeMiga.Tests.Features.Brands.Services {
    public class BrandServiceImplTests {
        // 📦 Función ayudante: Genera un contexto LIMPIO y ÚNICO cada vez que se la llama
        private ApplicationDbContext CreateInMemoryDbContext() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        // ⚙️ Función ayudante: Configura el AutoMapper real usando tu perfil original
        private IMapper CreateRealMapper() {
            var config = new MapperConfiguration(cfg => {
                cfg.AddProfile<BrandMappingProfile>(); // Usa tu mapeo real
            });
            return config.CreateMapper();
        }

        // =========================================================================
        // 🧪 PRUEBAS: CreateAsync
        // =========================================================================

        [Fact]
        public async Task CreateAsync_WhenBrandNameAlreadyExists_ShouldThrowAlreadyExistsException() {
            using var context = CreateInMemoryDbContext();
            // Agregamos una marca en minúsculas
            context.Brands.Add(new BrandEntity { Id = Guid.NewGuid(), Name = "bimbo", LogoUrl = "url" }); 
            await context.SaveChangesAsync();

            var service = new BrandServiceImpl(context, CreateRealMapper());
            // Intentamos crear la misma marca combinando Mayúsculas para probar el .ToLower()
            var request = new BrandRequestDto { Name = "BIMBO", LogoUrl = "url2" };

            var exception = await Assert.ThrowsAsync<AlreadyExistsException>(() => service.CreateAsync(request));
            Assert.Equal("el nombre 'BIMBO' ya esta registrado", exception.Message); 
        }

        [Fact]
        public async Task CreateAsync_WhenDataIsValid_ShouldSaveBrandAndReturnResponseDto() {
            using var context = CreateInMemoryDbContext();
            var service = new BrandServiceImpl(context, CreateRealMapper());
            var request = new BrandRequestDto { Name = "Fargo", LogoUrl = "http://logo.com" };

            var result = await service.CreateAsync(request);

            Assert.NotNull(result); 
            Assert.Equal("Fargo", result.Name); 
            
            // Verificamos que realmente persistió en el contexto en memoria
            var brandInDb = await context.Brands.FirstOrDefaultAsync(b => b.Name == "Fargo");
            Assert.NotNull(brandInDb); 
        }

        // =========================================================================
        // 🧪 PRUEBAS: GetByIdAsync
        // =========================================================================

        [Fact]
        public async Task GetByIdAsync_WhenBrandDoesNotExist_ShouldThrowNotFoundException() {
            using var context = CreateInMemoryDbContext();
            var service = new BrandServiceImpl(context, CreateRealMapper());
            var randomId = Guid.NewGuid();

            var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(randomId)); 
            Assert.Equal($"la marca con id '{randomId}' no existe", exception.Message); 
        }

        [Fact]
        public async Task GetByIdAsync_WhenBrandExists_ShouldReturnCorrectBrand() {
            using var context = CreateInMemoryDbContext();
            var brandId = Guid.NewGuid();
            context.Brands.Add(new BrandEntity { Id = brandId, Name = "Chocolinas", LogoUrl = "url" });
            await context.SaveChangesAsync();

            var service = new BrandServiceImpl(context, CreateRealMapper());

            var result = await service.GetByIdAsync(brandId);

            Assert.NotNull(result);
            Assert.Equal("Chocolinas", result.Name); 
        }

        // =========================================================================
        // 🧪 PRUEBAS: UpdateAsync
        // =========================================================================

        [Fact]
        public async Task UpdateAsync_WhenBrandDoesNotExist_ShouldThrowNotFoundException() {
            using var context = CreateInMemoryDbContext();
            var service = new BrandServiceImpl(context, CreateRealMapper());
            var request = new BrandRequestDto { Name = "Inexistente" };

            var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(Guid.NewGuid(), request));
        }

        [Fact]
        public async Task UpdateAsync_WhenNewNameIsAlreadyUsedByAnotherBrand_ShouldThrowAlreadyExistsException() {
            using var context = CreateInMemoryDbContext();
            var brandIdA = Guid.NewGuid();

            // Registramos dos marcas distintas[cite: 18]
            context.Brands.Add(new BrandEntity { Id = brandIdA, Name = "Marca A", LogoUrl = "url" }); 
            context.Brands.Add(new BrandEntity { Id = Guid.NewGuid(), Name = "Marca B", LogoUrl = "url" });
            await context.SaveChangesAsync();

            var service = new BrandServiceImpl(context, CreateRealMapper());
            // Intentamos renombrar la "Marca A" a "marca b" (ya ocupado por la otra entidad)
            var request = new BrandRequestDto { Name = "marca b", LogoUrl = "url" }; 

            var exception = await Assert.ThrowsAsync<AlreadyExistsException>(() => service.UpdateAsync(brandIdA, request)); 
            Assert.Equal("el nombre 'marca b' ya esta registrado", exception.Message); 
        }

        [Fact]
        public async Task UpdateAsync_WhenDataIsValid_ShouldModifyExistingBrandProperties() {
            using var context = CreateInMemoryDbContext();
            var brandId = Guid.NewGuid();
            context.Brands.Add(new BrandEntity { Id = brandId, Name = "Nombre Viejo", LogoUrl = "Url Vieja" }); 
            await context.SaveChangesAsync();

            var service = new BrandServiceImpl(context, CreateRealMapper());
            var request = new BrandRequestDto { Name = "Nombre Nuevo", LogoUrl = "Url Nueva" };

            var result = await service.UpdateAsync(brandId, request);

                Assert.Equal("Nombre Nuevo", result.Name);
            Assert.Equal("Url Nueva", result.LogoUrl); 
        }

        // =========================================================================
        // 🧪 PRUEBAS: DeleteAsync
        // =========================================================================

        [Fact]
        public async Task DeleteAsync_WhenBrandExists_ShouldRemoveFromDatabase() {
            using var context = CreateInMemoryDbContext();
            var brandId = Guid.NewGuid();
            context.Brands.Add(new BrandEntity { Id = brandId, Name = "Para Borrar", LogoUrl = "url" }); 

            var service = new BrandServiceImpl(context, CreateRealMapper());

            await service.DeleteAsync(brandId); 

            // Validamos que el conteo en la DB baje a cero
            var exists = await context.Brands.AnyAsync(b => b.Id == brandId);
            Assert.False(exists);
        }
    }
}