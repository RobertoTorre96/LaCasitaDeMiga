using AutoMapper;
using LaCasitaDeMiga.Features.Products.DTOs;

namespace LaCasitaDeMiga.Features.Products.mappers {
    public class ProductMappingProfile : Profile {

        public ProductMappingProfile() {

            // =========================================================
            // 1. MAPEOS DE ENTRADA (De DTO Request a Entidad)
            // =========================================================

            // De ProductoRequest a Entidad Padre
            CreateMap<ProducCreatetRequestDto, ProductEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // El ID lo genera la BD o el servicio (UUID)
                .ForMember(dest => dest.Slug, opt => opt.Ignore()) // El Slug lo calculamos siempre en el servicio de forma automática
                .ForMember(dest => dest.IsActive, opt => opt.Ignore()) // Se maneja por lógica de negocio (true por defecto)
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore()) // Evitamos pisar las propiedades de navegación de EF
                .ForMember(dest => dest.Brand, opt => opt.Ignore())
                // Mapeamos la lista de variantes anidada automáticamente
                .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.Variants));

            // De VarianteRequest a Entidad Hijo (Creación)
            CreateMap<ProductVariantRequestDto, ProductVariantEntity>()
    .ForMember(dest => dest.Id, opt => opt.Ignore())
    .ForMember(dest => dest.ProductId, opt => opt.Ignore())
    .ForMember(dest => dest.IsActive, opt => opt.Ignore())
    .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
    .ForMember(dest => dest.Product, opt => opt.Ignore())
    .ForMember(dest => dest.Version, opt => opt.Ignore())
    // --- MAPEO DIRECTO DEL COSTO INICIAL ---
    .ForMember(dest => dest.AverageCost, opt => opt.MapFrom(src => src.PurchasePrice))
    .ForMember(dest => dest.LastPurchasePrice, opt => opt.MapFrom(src => src.PurchasePrice))
    // ───────────────────────────────────────
    .ForMember(dest => dest.Sku, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Sku)))
    .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority ?? 0))
    .ForMember(dest => dest.IsFeatured, opt => opt.MapFrom(src => src.IsFeatured ?? false));

            // De UpdateProductVariantRequestDto a Entidad Hijo (Actualización)
            CreateMap<UpdateProductVariantRequestDto, ProductVariantEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ProductId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore())
                .ForMember(dest => dest.LastPurchasePrice, opt => opt.Ignore())
                .ForMember(dest => dest.AverageCost, opt => opt.Ignore())
                .ForMember(dest => dest.Version, opt => opt.Ignore()) // La versión se incrementa por código en el Service, la ignoramos aquí
                                                                      // --- MAPEO DE SKU CONDICIONAL (Evita pisar con null el SKU viejo de la BD) ---
                .ForMember(dest => dest.Sku, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Sku)))
                // --- MAPEO DE OPCIONALES CON VALORES POR DEFECTO ---
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority ?? 0))
                .ForMember(dest => dest.IsFeatured, opt => opt.MapFrom(src => src.IsFeatured ?? false));


            // =========================================================
            // 2. MAPEOS DE SALIDA (De Entidad a DTO Response)
            // =========================================================

            CreateMap<ProductEntity, ProductResponseDto>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Brand))
                .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.Variants));
            

            CreateMap<ProductVariantEntity, ProductVariantResponseDto>()
                .ForMember(dest=>dest.PurchasePrice,opt=>opt.MapFrom(src=>src.LastPurchasePrice));

            CreateMap<Features.Categories.CategoryEntity, ProductRelationDto>();
            CreateMap<Features.Brands.BrandEntity, ProductRelationDto>();
        }
    }
}