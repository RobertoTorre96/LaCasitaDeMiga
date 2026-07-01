namespace LaCasitaDeMiga.Features.Orders.DTOs {
    public class OrderItemResponseDto {

        public Guid Id { get; set; }
        public Guid ProductVariantId { get; set; }
        public string VariantName { get; set; } = null!; // Ej: "Remera Negra - Talle L"
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal => Quantity * UnitPrice; // Propiedad calculada al vuelo
    }
}
