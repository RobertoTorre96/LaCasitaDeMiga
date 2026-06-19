using ECommersAPI.Common.DTOs;
using ECommersAPI.Features.Products.DTOs;
using ECommersAPI.Features.Products.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ECommersAPI.Features.Products {
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase {

        private readonly IProductService _productService;

        public ProductController(IProductService productService) {
            _productService = productService;
        }

        // 1. OBTENER TODO (Paginado y Filtrado)
        // GET: api/products?categoryId=...&brandId=...&pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<ProductResponseDto>>> GetAll(
            [FromQuery] Guid? categoryId,
            [FromQuery] Guid? brandId,
            [FromQuery] bool onlyActive = true,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10) {

            var result = await _productService.GetAllAsync(categoryId, brandId, onlyActive, pageNumber, pageSize);
            return Ok(result);
        }

        // 2. OBTENER POR ID
        // GET: api/products/id/3fa85f64-5717-4562-b3fc-2c963f66afa6
        [HttpGet("id/{id:guid}")]
        public async Task<ActionResult<ProductResponseDto>> GetById(Guid id) {
            // Nota: GetByIdAsync ya lanza NotFoundException si no existe, 
            // por lo que el middleware responderá un 404 automáticamente.
            var product = await _productService.GetByIdAsync(id);
            return Ok(product);
        }

        // 3. OBTENER POR SLUG (Ideal para el Frontend de la tienda)
        // GET: api/products/slug/remera-oversize-negra
        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<ProductResponseDto>> GetBySlug(string slug) {
            var product = await _productService.GetBySlugAsync(slug);
            return Ok(product);
        }

        // 4. CREAR PRODUCTO Y VARIANTES
        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<ProductResponseDto>> Create([FromBody] ProductRequestDto request) {
            // .NET valida automáticamente las anotaciones como [Required] y [MinLength] 
            // gracias al atributo [ApiController] de la clase.
            var createdProduct = await _productService.CreateAsync(request);

            // Buena práctica REST: Devolver un 201 Created con la cabecera Location apuntando al GetById
            return Ok(createdProduct);
        }

        // 5. ACTUALIZAR PRODUCTO (Padre e hijos)
        // PUT: api/products/3fa85f64-5717-4562-b3fc-2c963f66afa6
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ProductResponseDto>> Update(Guid id, [FromBody] ProductRequestDto request) {
            var updatedProduct = await _productService.UpdateAsync(id, request);
            return Ok(updatedProduct);
        }

        // 6. BORRADO FISICO / LOGICO
        // DELETE: api/products/3fa85f64-5717-4562-b3fc-2c963f66afa6
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id) {
            await _productService.DeleteAsync(id);
            return NoContent(); // 204 No Content (Estándar REST para borrados exitosos)
        }

        // 7. ACTUALIZAR STOCK DE UNA VARIANTE
        // POST: api/products/variants/3fa85f64-5717-4562-b3fc-2c963f66afa6/stock
        [HttpPost("variants/{variantId:guid}/stock")]
        public async Task<IActionResult> UpdateStock(Guid variantId, [FromBody] int quantity) {
            var success = await _productService.UpdateStockAsync(variantId, quantity);

            if (!success) {
                return BadRequest(new { message = "No se pudo impactar el cambio de stock en la base de datos." });
            }

            return Ok(new { message = "Stock actualizado correctamente." });
        }
    }
}
