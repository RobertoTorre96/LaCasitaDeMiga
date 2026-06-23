using AutoMapper;
using LaCasitaDeMiga.Features.Products.DTOs;

namespace LaCasitaDeMiga.Features.Products.mappers {
    public class ProductMappingProfile : Profile {

        public ProductMappingProfile() {

            // =========================================================
            // 1. MAPEOS DE ENTRADA (De DTO Request a Entidad)
            // =========================================================

            // De ProductoRequest a Entidad Padre
            CreateMap<ProductRequestDto, ProductEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // El ID lo genera la BD o el servicio (UUID)
                .ForMember(dest => dest.Slug, opt => opt.Ignore()) // El Slug lo calculamos en el servicio
                .ForMember(dest => dest.IsActive, opt => opt.Ignore()) // Se maneja por lógica de negocio (true por defecto)
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore()) // Evitamos pisar las propiedades de navegación de EF
                .ForMember(dest => dest.Brand, opt => opt.Ignore())
                // Mapeamos la lista de variantes anidada automáticamente
                .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.Variants));

            // De VarianteRequest a Entidad Hijo
            CreateMap<ProductVariantRequestDto, ProductVariantEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ProductId, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore())
                // --- CORRECCIONES NUEVAS (Ignorados en la Entrada) ---
                .ForMember(dest => dest.Sku, opt => opt.Ignore())              // Lo genera el Service automáticamente
                .ForMember(dest => dest.LastPurchasePrice, opt => opt.Ignore())// Nace en 0 hasta la primera compra
                .ForMember(dest => dest.AverageCost, opt => opt.Ignore());     // Nace en 0 hasta la primera compra


            // =========================================================
            // 2. MAPEOS DE SALIDA (De Entidad a DTO Response)
            // =========================================================

            // De Producto Padre a ProductResponseDto
            CreateMap<ProductEntity, ProductResponseDto>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Brand))
                .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.Variants));

            // De Variante Hijo a ProductVariantResponseDto
            // Nota: IsLowStock, LastPurchasePrice y AverageCost se mapean automáticamente por coincidencia de nombres
            CreateMap<ProductVariantEntity, ProductVariantResponseDto>();

            // Para las relaciones simples de Categoría y Marca hacia el ProductRelationDto
            CreateMap<Features.Categories.CategoryEntity, ProductRelationDto>();
            CreateMap<Features.Brands.BrandEntity, ProductRelationDto>();
        }
    }
}