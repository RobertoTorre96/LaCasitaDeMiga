using LaCasitaDeMiga.Features.Delivery.DTOs;
using LaCasitaDeMiga.Features.Delivery.Enums;
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
        /// Valida la dirección ingresada y retorna la zona de envío asignada.
        /// </summary>
        [HttpGet("validate")]
        public async Task<IActionResult> ValidateAddress([FromQuery] DeliveryLookupRequestDto request) {
            EDeliveryZone zone = await _deliveryService.GetDeliveryZoneAsync(request.Address);

            bool isWithinZone = zone != EDeliveryZone.OutOfZone;

            string message = zone switch {
                EDeliveryZone.Zone1 => "¡Excelente! Estás en la Zona 1 (Cercana).",
                EDeliveryZone.Zone2 => "¡Genial! Estás en la Zona 2 (Media).",
                EDeliveryZone.Zone3 => "¡Estás dentro de la cobertura! Zona 3 (Límite 15Km).",
                _ => "Lo sentimos, la dirección se encuentra fuera de nuestra zona de cobertura (15 Km)."
            };

            return Ok(new {
                Address = request.Address,
                IsWithinZone = isWithinZone,
                Zone = (int)zone,          // Devolverá: 1, 2, 3 o -1
                ZoneName = zone.ToString(), // Devolverá: "Zone1", "Zone2", "Zone3" o "OutOfZone"
                Message = message
            });
        }
    }
}