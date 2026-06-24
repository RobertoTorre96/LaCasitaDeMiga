using LaCasitaDeMiga.Features.Users.DTOs;
using LaCasitaDeMiga.Features.Users.services;
using Microsoft.AspNetCore.Mvc;

namespace LaCasitaDeMiga.Features.Users.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class UserController :ControllerBase{

        private readonly IUserService _userService;

        public UserController(IUserService userService) {
            _userService = userService;
        }

        /// <summary>
        /// Actualiza el rol y el estado de actividad (IsActive) de un usuario.
        /// </summary>
        /// <param name="id">El Guid único del usuario a modificar.</param>
        /// <param name="request">DTO con el nuevo estado y rol.</param>
        [HttpPut("{id}/status-role")]
        public async Task<IActionResult> UpdateStatusAndRole(Guid id, [FromBody] UserUpdateRequestDto request) {
            // .NET valida automáticamente las anotaciones del DTO ([Required], [EnumDataType])
            // Si algo falla, el framework responde automáticamente con un 400 Bad Request.

            bool isUpdated = await _userService.UpdateStatusAndRoleAsync(id, request);

            if (!isUpdated) {
                return NotFound(new { Message = "El usuario especificado no fue encontrado." });
            }

            return Ok(new { Message = "El usuario ha sido actualizado correctamente." });
        }

        /// <summary>
        /// Obtiene la lista de todos los roles disponibles en el sistema.
        /// </summary>
        [HttpGet("roles")]
        public IActionResult GetRoles() {
            var roles = _userService.GetAvailableRoles();
            return Ok(roles);
        }

    }
}
