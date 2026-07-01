using LaCasitaDeMiga.Exceptions;
using LaCasitaDeMiga.Features.Categories.DTOs;
using LaCasitaDeMiga.Data;
using Microsoft.EntityFrameworkCore;

namespace LaCasitaDeMiga.Features.Categories.Services {
    public class CategoryServiceImpl : ICategoryService {

        private readonly ApplicationDbContext _context;

        public CategoryServiceImpl(ApplicationDbContext context) {
            _context = context;
        }

        public async Task<CategoryResponseDto> CreateAsync(CategoryRequestDto request) {
            string finalSlug = GenerateSlug(request.Name);

            // 1. Si no tiene padre, es raíz. Creamos la entidad directo y salimos del método.
            if (!request.ParentId.HasValue) {
                return await SaveAndReturnCategoryAsync(request.Name, finalSlug, null);
            }

            // 2. Si tiene padre, lo buscamos. Si no existe, disparamos la guarda inmediatamente.
            var parentCategory = await _context.Categories.FindAsync(request.ParentId.Value);
            if (parentCategory == null) {
                throw new NotFoundException($"La categoría padre con ID '{request.ParentId.Value}' no existe.");
            }

            // 3. Si el padre tiene padre, validamos la profundidad con el abuelo.
            if (parentCategory.ParentId.HasValue) {
                var grandparentCategory = await _context.Categories.FindAsync(parentCategory.ParentId.Value);

                if (grandparentCategory != null && grandparentCategory.ParentId.HasValue) {
                    throw new BadRequestException("No se permiten más de 3 niveles de profundidad en las categorías.");
                }
            }

            // 4. Si todo está bien, combinamos el slug con el del padre
            finalSlug = $"{parentCategory.Slug}-{finalSlug}";

            return await SaveAndReturnCategoryAsync(request.Name, finalSlug, request.ParentId);
        }

        public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync() {
            Console.WriteLine("metodo getAllAsync llamado");

            var entities = await _context.Categories.ToListAsync();

            return entities.Select(c => new CategoryResponseDto {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                ParentId = c.ParentId,
                CreatedAt = c.CreatedAt
            });
        }


        public async Task<CategoryResponseDto> GetByIdAsync(Guid id) {

            var entity = await _context.Categories.FindAsync(id);

            if (entity == null) {
                throw new NotFoundException($"La categoría con ID '{id}' no existe.");
            }

            return new CategoryResponseDto {
                Id = entity.Id,
                Name = entity.Name,
                ParentId = entity.ParentId,
                Slug = entity.Slug,
                CreatedAt = entity.CreatedAt
            };
        }

        public async Task<CategoryResponseDto> UpdateAsync(Guid id, CategoryRequestDto request) {
            // 1. Validar que la categoría a editar exista
            var category = await _context.Categories.FindAsync(id);
            if (category == null) {
                throw new NotFoundException($"La categoría con ID '{id}' no existe.");
            }

            // 2. Una categoría no puede ser su propio padre
            if (request.ParentId.HasValue && request.ParentId.Value == id) {
                throw new BadRequestException("Una categoría no puede ser hija de sí misma.");
            }

            string newSlugBase = GenerateSlug(request.Name);

            // 3. Calcular el slug final (usando el nuevo método extraído si tiene padre)
            string finalSlug = request.ParentId.HasValue
                ? await ProcessParentCategoryAsync(id, request.ParentId.Value, newSlugBase)
                : newSlugBase;

            // 4. Validar que el slug no esté duplicado en OTRA categoría
            var slugExists = await _context.Categories.AnyAsync(c => c.Slug == finalSlug && c.Id != id);
            if (slugExists) {
                throw new AlreadyExistsException("El slug resultante ya está en uso por otra categoría.");
            }

            // 5. Si el slug cambió, actualizamos los hijos en cascada
            if (category.Slug != finalSlug) {
                await UpdateChildrenSlugsAsync(category.Slug, finalSlug);
            }

            // 6. Aplicar cambios y guardar
            category.Name = request.Name;
            category.Slug = finalSlug;
            category.ParentId = request.ParentId;

            await _context.SaveChangesAsync();

            return new CategoryResponseDto {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                ParentId = category.ParentId,
                CreatedAt = category.CreatedAt
            };
        }
        public async Task DeleteAsync(Guid id) {
            // 1. Guarda: Validar que la categoría exista
            var category = await _context.Categories.FindAsync(id);
            if (category == null) {
                throw new NotFoundException($"La categoría con ID '{id}' no existe.");
            }

            // 2. Guarda: Validar si la categoría tiene subcategorías hijas
            var hasChildren = await _context.Categories.AnyAsync(c => c.ParentId == id);
            if (hasChildren) {
                throw new BadRequestException("No se puede eliminar la categoría porque tiene subcategorías asociadas.");
            }

            // 3. Si pasa las validaciones, la removemos de la base de datos
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
        //-----------------------------------------------------------------------------------------------------------------------------------
        //  helpers methods

        private async Task UpdateChildrenSlugsAsync(string oldParentSlug, string newParentSlug) {
            // Buscamos todas las categorías cuyo slug empiece con el slug viejo
            var childCategories = await _context.Categories
                .Where(c => c.Slug.StartsWith(oldParentSlug + "-"))
                .ToListAsync();

            foreach (var child in childCategories) {
                // Reemplazamos la parte vieja del slug por la nueva
                child.Slug = child.Slug.Replace(oldParentSlug, newParentSlug);
            }
        }

        private string GenerateSlug(string frase) {
            // Pasa a minúsculas, quita espacios extras y reemplaza espacios por guiones
            string texto = frase.ToLower().Trim();
            texto = System.Text.RegularExpressions.Regex.Replace(texto, @"\s+", "-");
            // Remueve caracteres raros
            texto = System.Text.RegularExpressions.Regex.Replace(texto, @"[^a-z0-9-]", "");
            return texto;
        }

        private async Task<CategoryResponseDto> SaveAndReturnCategoryAsync(string name, string slug, Guid? parentId) {
            var slugExists = await _context.Categories.AnyAsync(c => c.Slug == slug);
            if (slugExists) {
                throw new AlreadyExistsException("El slug ya está en uso por otra categoría.");
            }

            var newEntity = new CategoryEntity {
                Id = Guid.NewGuid(),
                Name = name,
                Slug = slug,
                ParentId = parentId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(newEntity);
            await _context.SaveChangesAsync();

            return new CategoryResponseDto {
                Id = newEntity.Id,
                Name = newEntity.Name,
                Slug = newEntity.Slug,
                ParentId = newEntity.ParentId,
                CreatedAt = newEntity.CreatedAt
            };
        }

        private async Task<string> ProcessParentCategoryAsync(Guid currentCategoryId, Guid parentId, string newSlugBase) {
            // 1. Buscar que la categoría padre exista
            var parentCategory = await _context.Categories.FindAsync(parentId);
            if (parentCategory == null) {
                throw new NotFoundException($"La categoría padre con ID '{parentId}' no existe.");
            }

            // 2. Evitar bucles (que el nuevo padre sea un hijo directo de la categoría actual)
            if (parentCategory.ParentId.HasValue && parentCategory.ParentId.Value == currentCategoryId) {
                throw new BadRequestException("Ciclo detectado: El padre seleccionado ya es un hijo de esta categoría.");
            }

            // 3. Validar profundidad máxima (3 niveles) con el abuelo
            if (parentCategory.ParentId.HasValue) {
                var grandparentCategory = await _context.Categories.FindAsync(parentCategory.ParentId.Value);

                if (grandparentCategory != null && grandparentCategory.ParentId.HasValue) {
                    throw new BadRequestException("No se permiten más de 3 niveles de profundidad en las categorías.");
                }
            }

            // Si todo está bien, devolvemos el slug combinado
            return $"{parentCategory.Slug}-{newSlugBase}";
        }



    }

}
