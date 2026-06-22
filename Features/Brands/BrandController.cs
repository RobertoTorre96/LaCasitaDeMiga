using LaCasitaDeMiga.Features.Brands.DTOs;
using LaCasitaDeMiga.Features.Brands.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaCasitaDeMiga.Features.Brands {
    [ApiController]
    [Route("api/Brands")]
    public class BrandController :ControllerBase{
        private readonly IBrandService _brandService;

        public BrandController( IBrandService brandService) {
            _brandService = brandService;
        }

        [HttpPost]
        public async Task<IActionResult> Created([FromBody] BrandRequestDto request) {
            var response = await _brandService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById),new {id=response.Id},response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() {
            var brands = await _brandService.GetAllAsync();
            return Ok(brands);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id) {
            var brand = await _brandService.GetByIdAsync(id);
            return Ok(brand);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] BrandRequestDto request) {
            var updatedBrand = await _brandService.UpdateAsync(id, request);
            return Ok(updatedBrand);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id) {
            await _brandService.DeleteAsync(id);
            return NoContent(); 
        }



    }
}
