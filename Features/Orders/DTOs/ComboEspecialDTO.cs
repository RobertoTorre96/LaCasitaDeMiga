using System;
using System.ComponentModel.DataAnnotations;

namespace LaCasitaDeMiga.Features.Orders.DTOs {

    public class ComboEspecialDTO {

        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "El cliente es obligatorio.")]
        public Guid CustomerId { get; set; }

        public int CantComunes { get; set; }
        public decimal PriceComunes { get; set; }

        // ◄ El frontend no lo envía, se calcula solo al vuelo
        public decimal SubTotalComunes => CantComunes * PriceComunes;

        public int CantEspeciales { get; set; }
        public decimal PriceEspeciales { get; set; }

        // ◄ El frontend no lo envía, se calcula solo al vuelo
        public decimal SubTotalEspeciales => CantEspeciales * PriceEspeciales;

        // ◄ Suma automática de ambos subtotales
        public decimal TotalAmount => SubTotalComunes + SubTotalEspeciales;
    }
}