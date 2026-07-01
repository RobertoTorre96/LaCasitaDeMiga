using LaCasitaDeMiga.Features.Categories.DTOs;
using LaCasitaDeMiga.Features.Categories.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaCasitaDeMiga.Features.Categories {

    [ApiController]
    [Route("api/categories")]
    public class CategoryController : ControllerBase {

        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService) {
            _categoryService = categoryService;
        }

        [HttpPost]
        public async Task<IActionResult> Created([FromBody] CategoryRequestDto request) {

            var response = await _categoryService.CreateAsync(request);
            return Ok( response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() {
            var catrgories = await _categoryService.GetAllAsync();
            return Ok(catrgories);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id) {
            var category = await _categoryService.GetByIdAsync(id);
            return Ok(category);
        }

        
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] CategoryRequestDto request) {
            var updatedCategory = await _categoryService.UpdateAsync(id, request);
            return Ok(updatedCategory);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id) {
            await _categoryService.DeleteAsync(id);
            return NoContent(); // Buena práctica REST: Devolver 204 NoContent al borrar con éxito
        }

    }
}
