using LaCasitaDeMiga.Features.Delivery.DTOs;
using LaCasitaDeMiga.Features.Delivery.services;
using Microsoft.AspNetCore.Mvc;

namespace LaCasitaDeMiga.Features.Delivery.Controller {

    [ApiController]
    [Route("api/delivery")]
    public class DeliveryController : ControllerBase {


        private readonly IDeliveryService _deliveryService;

        public DeliveryController(IDeliveryService deliveryService) {
            _deliveryService = deliveryService;
        }

        /// <summary>
        /// Valida si una dirección ingresada está dentro del radio de 15Km de la Casita de Miga.
        /// </summary>
        [HttpGet("validate")]
        public async Task<IActionResult> ValidateAddress([FromQuery] DeliveryLookupRequestDto request) {
            // NOTA: No hace falta el 'if (string.IsNullOrWhiteSpace)', 
            // .NET valida automáticamente el [Required] del DTO antes de entrar acá.

            bool isWithinZone = await _deliveryService.IsAddressInDeliveryZoneAsync(request.Address);

            return Ok(new {
                Address = request.Address,
                IsWithinZone = isWithinZone,
                Message = isWithinZone
                    ? "¡Estás dentro de la zona de envío de La Casita de Miga!"
                    : "Lo sentimos, la dirección se encuentra fuera de nuestra zona de cobertura (15 Km)."
            });
        }


    }
}
