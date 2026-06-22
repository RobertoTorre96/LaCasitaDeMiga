using AutoMapper;
using LaCasitaDeMiga.Features.Orders.DTOs;

namespace LaCasitaDeMiga.Features.Orders.Mapper {
    public class OrderMappingProfile : Profile{
        public OrderMappingProfile() {

            // 1. Mapeo de la Cabecera de la Orden
            CreateMap<OrderEntity, OrderResponseDto>()
                 .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                 .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))

                 // --- NUEVO MAPEO ---
                 .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : "Invitado"))
                 .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Email : "Sin Email"));

            // 2. Mapeo del Detalle (Ítem)
            CreateMap<OrderItemEntity, OrderItemResponseDto>()
                // Como la entidad tiene relación con la variante, podemos sacar el nombre 
                // combinando el nombre del producto padre y las características de la variante.
                // Ej: "Remera Oversize - Negro / XL"
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src =>
                    src.ProductVariant != null && src.ProductVariant.Product != null
                        ? $"{src.ProductVariant.Product.Name} - {src.ProductVariant.Sku}"
                        : "Producto no especificado"));
        }
    }
}
