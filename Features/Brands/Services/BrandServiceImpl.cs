using AutoMapper;
using ECommerceAPI.Data;
using ECommersAPI.Exceptions;
using ECommersAPI.Features.Brands.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ECommersAPI.Features.Brands.Services {
    public class BrandServiceImpl :IBrandService {

        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public BrandServiceImpl(ApplicationDbContext context, IMapper mapper) {
            _mapper = mapper;
            _context = context;
        }



        public async Task<BrandResponseDto> CreateAsync(BrandRequestDto request) {
            var brandEntity = _mapper.Map<BrandEntity>(request);
            
            var exist=await _context.Brands.AnyAsync((b)=>request.Name.ToLower()==b.Name.ToLower());
            if (exist) {
                throw new AlreadyExistsException($"el nombre '{request.Name}' ya esta registrado");
            }
            _context.Brands.Add(brandEntity);
            await _context.SaveChangesAsync();
            return _mapper.Map<BrandResponseDto>(brandEntity);
        }


        public async Task<IEnumerable<BrandResponseDto>> GetAllAsync() {
            var brands = await _context.Brands.ToListAsync();

            return _mapper.Map<IEnumerable<BrandResponseDto>>(brands);
        }

        public async  Task<BrandResponseDto> GetByIdAsync(Guid id) {
            var brand =  await _context.Brands.FindAsync( id);
            if (brand == null) {
                throw new NotFoundException($"la marca con id '{id}' no existe");
            }
            return _mapper.Map<BrandResponseDto>(brand);
        }

        public async Task<BrandResponseDto> UpdateAsync(Guid id, BrandRequestDto request) {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) {
                throw new NotFoundException($"la marca con id '{id}' no existe");
            }

            var nameExists= await _context.Brands.AnyAsync(b => b.Id != id && b.Name.ToLower() == request.Name.ToLower());
            if (nameExists) {
                throw new AlreadyExistsException($"el nombre '{request.Name}' ya esta registrado");
            }

             _mapper.Map(request, brand);            
            await _context.SaveChangesAsync();

            return _mapper.Map<BrandResponseDto>(brand);
        }

        public async Task DeleteAsync(Guid id) {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) {
                throw new NotFoundException($"la marca con id '{id}' no existe");
            }
            _context.Brands.Remove(brand);
           await _context.SaveChangesAsync();
        }
    }
}
