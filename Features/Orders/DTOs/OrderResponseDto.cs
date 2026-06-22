namespace LaCasitaDeMiga.Features.Orders.DTOs {
    public class OrderResponseDto {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }

        // --- NUEVOS CAMPOS (OPCIONALES) ---
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        // ----------------------------------

        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new();
    }
}