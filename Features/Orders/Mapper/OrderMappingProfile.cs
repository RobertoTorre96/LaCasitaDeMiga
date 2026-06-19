using AutoMapper;
using ECommersAPI.Features.Orders.DTOs;

namespace ECommersAPI.Features.Orders.Mapper {
    public class OrderMappingProfile : Profile{
        public OrderMappingProfile() {

            // 1. Mapeo de la Cabecera de la Orden
            CreateMap<OrderEntity, OrderResponseDto>()
                // Convertimos el Enum a su representación en String para el Frontend
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                // Nos aseguramos de que mapee la lista de ítems hijos
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

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
