using LaCasitaDeMiga.Common.DTOs;
using LaCasitaDeMiga.Features.Products.DTOs;
using LaCasitaDeMiga.Features.Products.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaCasitaDeMiga.Features.Products {
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase {

        private readonly IProductService _productService;

        public ProductController(IProductService productService) {
            this._productService = productService;
        }

        // 1. OBTENER TODO (Paginado y Filtrado)
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
        [HttpGet("id/{id:guid}", Name = "GetProductById")]
        public async Task<ActionResult<ProductResponseDto>> GetById(Guid id) {
            var product = await _productService.GetByIdAsync(id);
            return Ok(product);
        }

        // 3. OBTENER POR SLUG
        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<ProductResponseDto>> GetBySlug(string slug) {
            var product = await _productService.GetBySlugAsync(slug);
            return Ok(product);
        }

        // 4. CREAR PRODUCTO Y VARIANTES INICIALES
        [HttpPost]
        public async Task<ActionResult<ProductResponseDto>> Create([FromBody] ProducCreatetRequestDto request) {
            var createdProduct = await _productService.CreateAsync(request);
            // Estándar REST profesional: Devuelve Estado 210 Created y la URL de acceso directo en las cabeceras
            return CreatedAtRoute("GetProductById", new { id = createdProduct.Id }, createdProduct);
        }

        // 5. ACTUALIZAR PRODUCTO (Campos generales del Padre)
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ProductResponseDto>> Update(Guid id, [FromBody] ProductUpdateDto request) {
            var updatedProduct = await _productService.UpdateAsync(id, request);
            return Ok(updatedProduct);
        }

        // 6. ELIMINAR PRODUCTO (Y variantes asociadas)
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id) {
            await _productService.DeleteAsync(id);
            return NoContent();
        }

        // 7. [NUEVO] AGREGAR NUEVAS VARIANTES A UN PRODUCTO EXISTENTE
        // POST: api/products/{id}/variants
        [HttpPost("{id:guid}/variants")]
        public async Task<IActionResult> AddVariants(Guid id, [FromBody] AddProductVariantsRequestDto dto) {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updatedProduct = await _productService.AddVariantsAsync(id, dto);
            return Ok(updatedProduct);
        }

        // 8. CORREGIDO A POST: ACTUALIZAR STOCK DE UNA VARIANTE (Ajustes manuales acumulativos)
        // POST: api/products/variants/{variantId}/stock
        [HttpPost("variants/{variantId:guid}/stock")]
        public async Task<IActionResult> UpdateStock(Guid variantId, [FromBody] int quantity) {
            var success = await _productService.UpdateStockAsync(variantId, quantity);

            if (!success) {
                return BadRequest(new { message = "No se pudo impactar el cambio de stock en la base de datos." });
            }

            return Ok(new { message = "Stock ajustado correctamente." });
        }

        // 9. CORREGIDO A POST: REGISTRAR INGRESO DE STOCK PROVEEDOR (Costo Promedio Ponderado)
        // POST: api/products/variants/{variantId}/stock-entry
        [HttpPost("variants/{variantId:guid}/stock-entry")]
        public async Task<IActionResult> RegisterStockEntry(Guid variantId, [FromBody] StockEntryRequestDto request) {
            var success = await _productService.RegisterStockEntryAsync(
                variantId,
                request.QuantityReceived,
                request.PurchasePrice
            );

            if (!success) {
                return BadRequest(new { message = "No se pudo registrar el ingreso de mercadería en la base de datos." });
            }

            return Ok(new { message = "Ingreso de stock registrado y costo promedio recalculado con éxito." });
        }

        // 10. ACTUALIZAR DETALLES DE UNA VARIANTE ESPECÍFICA (Precios, atributos, etc.)
        // PUT: api/products/variants/{variantId}
        [HttpPut("variants/{variantId:guid}")]
        public async Task<ActionResult<ProductVariantResponseDto>> UpdateVariant(
            Guid variantId,
            [FromBody] UpdateProductVariantRequestDto dto) {

            var updatedVariant = await _productService.UpdateVariantAsync(variantId, dto);
            return Ok(updatedVariant);
        }
    }
}