using AutoMapper;
using LaCasitaDeMiga.Features.Orders.DTOs;

namespace LaCasitaDeMiga.Features.Orders.Mapper {
    public class OrderMappingProfile : Profile {
        public OrderMappingProfile() {

            // =========================================================
            // 1. MAPEOS DE ENTRADA (De DTO Request a Entidad)
            // =========================================================

            // De la cabecera del Request a la Entidad principal
            CreateMap<OrderRequestDto, OrderEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())       // Lo genera C# al nacer la entidad (Guid.NewGuid())
                .ForMember(dest => dest.Status, opt => opt.Ignore())   // Nace en 'Pending' por defecto en la entidad
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore()) // Lo calcula el servicio sumando los subtotales
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())   // Se asigna la fecha actual en el servidor
                .ForMember(dest => dest.Customer, opt => opt.Ignore())    // Propiedad de navegación de EF Core
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            // De los ítems del Request al renglón de la base de datos
            CreateMap<OrderItemRequestDto, OrderItemEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OrderId, opt => opt.Ignore())
                .ForMember(dest => dest.Order, opt => opt.Ignore())
                .ForMember(dest => dest.ProductVariant, opt => opt.Ignore())
                // --- 🔒 PROTECCIÓN DE PRECIOS Y COSTOS ---
                .ForMember(dest => dest.UnitPrice, opt => opt.Ignore())  // Lo inyecta el service buscando en la BD
                .ForMember(dest => dest.UnitCost, opt => opt.Ignore());  // Lo inyecta el service para las ganancias


            // =========================================================
            // 2. MAPEOS DE SALIDA (De Entidad a DTO Response)
            // =========================================================

            // Mapeo de la Cabecera de la Orden
            CreateMap<OrderEntity, OrderResponseDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            // Mapeo del Detalle (Ítem)
            CreateMap<OrderItemEntity, OrderItemResponseDto>()
                // Construcción inteligente del nombre para que el cliente entienda qué compró
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src =>
                    src.ProductVariant != null && src.ProductVariant.Product != null
                        ? $"{src.ProductVariant.Product.Name} ({string.Join(", ", src.ProductVariant.Attributes.Values)})"
                        : "Producto no especificado"));
        }
    }
}