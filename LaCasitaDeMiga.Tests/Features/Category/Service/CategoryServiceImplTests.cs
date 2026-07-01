using Xunit;
using Microsoft.EntityFrameworkCore;
using LaCasitaDeMiga.Data;
using LaCasitaDeMiga.Exceptions;
using LaCasitaDeMiga.Features.Categories;
using LaCasitaDeMiga.Features.Categories.DTOs;
using LaCasitaDeMiga.Features.Categories.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LaCasitaDeMiga.Tests.Features.Categories.Services {
    public class CategoryServiceImplTests {
        // 📦 Función ayudante: Genera un contexto LIMPIO y ÚNICO cada vez que se la llama
        private ApplicationDbContext CreateInMemoryDbContext() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Nombre único aleatorio
                .Options;

            return new ApplicationDbContext(options);
        }

        // =========================================================================
        // 📁 PRUEBAS: CreateAsync (Jerarquías y Profundidad)
        // =========================================================================
        [Fact]
        public async Task CreateAsync_WhenRootCategoryAndSlugExists_ShouldThrowAlreadyExistsException() {
            using var context = CreateInMemoryDbContext();

            // 💡 Escribimos "Panaderia" sin tilde para garantizar coincidencia exacta en las strings
            context.Categories.Add(new CategoryEntity { Id = Guid.NewGuid(), Name = "Panaderia", Slug = "panaderia" });
            await context.SaveChangesAsync();

            var service = new CategoryServiceImpl(context);
            var request = new CategoryRequestDto { Name = "Panaderia", ParentId = null }; // 💡 Sin tilde aquí también

            var exception = await Assert.ThrowsAsync<AlreadyExistsException>(() => service.CreateAsync(request));
            Assert.Equal("El slug ya está en uso por otra categoría.", exception.Message);
        }
        [Fact]
        public async Task CreateAsync_WhenParentDoesNotExist_ShouldThrowNotFoundException() {
            using var context = CreateInMemoryDbContext();
            var service = new CategoryServiceImpl(context);
            var randomParentId = Guid.NewGuid();
            var request = new CategoryRequestDto { Name = "Facturas", ParentId = randomParentId };

            var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(request));
            Assert.Equal($"La categoría padre con ID '{randomParentId}' no existe.", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_WhenExceedsThreeLevelsOfDepth_ShouldThrowBadRequestException() {
            using var context = CreateInMemoryDbContext();

            var abueloId = Guid.NewGuid();
            var padreId = Guid.NewGuid();

            // Simulamos 3 niveles existentes: Bisabuelo -> Abuelo -> Padre
            context.Categories.Add(new CategoryEntity { Id = Guid.NewGuid(), Name = "Bisabuelo", Slug = "bisabuelo", ParentId = null });
            context.Categories.Add(new CategoryEntity { Id = abueloId, Name = "Abuelo", Slug = "abuelo", ParentId = Guid.NewGuid() }); // Tiene un padre, actúa como nivel 2
            context.Categories.Add(new CategoryEntity { Id = padreId, Name = "Padre", Slug = "padre", ParentId = abueloId }); // Nivel 3
            await context.SaveChangesAsync();

            var service = new CategoryServiceImpl(context);
            // Intentamos meter un 4to nivel
            var request = new CategoryRequestDto { Name = "Hijo Invalido", ParentId = padreId };

            var exception = await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(request));
            Assert.Equal("No se permiten más de 3 niveles de profundidad en las categorías.", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_WhenValidSubcategory_ShouldCombineSlugs() {
            using var context = CreateInMemoryDbContext();
            var padreId = Guid.NewGuid();
            context.Categories.Add(new CategoryEntity { Id = padreId, Name = "Panadería", Slug = "panaderia", ParentId = null });
            await context.SaveChangesAsync();

            var service = new CategoryServiceImpl(context);
            var request = new CategoryRequestDto { Name = "Minitartas Dulces", ParentId = padreId };

            var result = await service.CreateAsync(request);

            Assert.NotNull(result);
            Assert.Equal("panaderia-minitartas-dulces", result.Slug); // Valida la generación combinada del Slug
        }

        // =========================================================================
        // 🔄 PRUEBAS: UpdateAsync (Ciclos y Cascadas)
        // =========================================================================

        [Fact]
        public async Task UpdateAsync_WhenCategoryIsItsOwnParent_ShouldThrowBadRequestException() {
            using var context = CreateInMemoryDbContext();
            var categoryId = Guid.NewGuid();
            context.Categories.Add(new CategoryEntity { Id = categoryId, Name = "Categoría", Slug = "categoria" });
            await context.SaveChangesAsync();

            var service = new CategoryServiceImpl(context);
            // Intentamos asignarse a sí misma como padre
            var request = new CategoryRequestDto { Name = "Categoría Editada", ParentId = categoryId };

            var exception = await Assert.ThrowsAsync<BadRequestException>(() => service.UpdateAsync(categoryId, request));
            Assert.Equal("Una categoría no puede ser hija de sí misma.", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_WhenLoopDetected_ShouldThrowBadRequestException() {
            using var context = CreateInMemoryDbContext();
            var hijoId = Guid.NewGuid();
            var padreId = Guid.NewGuid();

            // Configuración del ciclo: El padre actual tiene como ParentId al hijo
            context.Categories.Add(new CategoryEntity { Id = hijoId, Name = "Hijo", Slug = "hijo", ParentId = null });
            context.Categories.Add(new CategoryEntity { Id = padreId, Name = "Padre Loco", Slug = "padre-loco", ParentId = hijoId });
            await context.SaveChangesAsync();

            var service = new CategoryServiceImpl(context);
            // Intentamos actualizar al "Hijo" diciendo que su nuevo padre es "Padre Loco" (Ciclo)
            var request = new CategoryRequestDto { Name = "Hijo Modificado", ParentId = padreId };

            var exception = await Assert.ThrowsAsync<BadRequestException>(() => service.UpdateAsync(hijoId, request));
            Assert.Equal("Ciclo detectado: El padre seleccionado ya es un hijo de esta categoría.", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_WhenSlugChanges_ShouldUpdateChildrenSlugsInCascade() {
            using var context = CreateInMemoryDbContext();
            var padreId = Guid.NewGuid();
            var hijoId = Guid.NewGuid();

            context.Categories.Add(new CategoryEntity { Id = padreId, Name = "Panes", Slug = "panes" });
            context.Categories.Add(new CategoryEntity { Id = hijoId, Name = "Casero", Slug = "panes-casero", ParentId = padreId });
            await context.SaveChangesAsync();

            var service = new CategoryServiceImpl(context);
            // Cambiamos el nombre de "Panes" a "Baguettes", lo que debería cambiar el slug a "baguettes"
            var request = new CategoryRequestDto { Name = "Baguettes", ParentId = null };

            await service.UpdateAsync(padreId, request);

            // Verificamos si el hijo se actualizó en cascada automáticamente ("panes-casero" -> "baguettes-casero")
            var childInDb = await context.Categories.FindAsync(hijoId);
            Assert.Equal("baguettes-casero", childInDb!.Slug);
        }

        // =========================================================================
        // 🗑️ PRUEBAS: DeleteAsync
        // =========================================================================

        [Fact]
        public async Task DeleteAsync_WhenCategoryHasChildren_ShouldThrowBadRequestException() {
            using var context = CreateInMemoryDbContext();
            var padreId = Guid.NewGuid();

            context.Categories.Add(new CategoryEntity { Id = padreId, Name = "Cafetería", Slug = "cafeteria" });
            context.Categories.Add(new CategoryEntity { Id = Guid.NewGuid(), Name = "Café en Granos", Slug = "cafeteria-granos", ParentId = padreId });
            await context.SaveChangesAsync();

            var service = new CategoryServiceImpl(context);

            // Intentamos borrar la categoría padre sin sacar al hijo primero
            var exception = await Assert.ThrowsAsync<BadRequestException>(() => service.DeleteAsync(padreId));
            Assert.Equal("No se puede eliminar la categoría porque tiene subcategorías asociadas.", exception.Message);
        }
    }
}