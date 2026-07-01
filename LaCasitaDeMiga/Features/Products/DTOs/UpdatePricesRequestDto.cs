namespace LaCasitaDeMiga.Features.Products.DTOs {
    public class UpdatePricesRequestDto {
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; } // Opcional
    }
}
