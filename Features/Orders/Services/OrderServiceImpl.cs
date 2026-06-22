using AutoMapper;
using LaCasitaDeMiga.Exceptions;
using LaCasitaDeMiga.Features.Orders.DTOs;
using LaCasitaDeMiga.Features.Products.Services;
using LaCasitaDeMiga.Data;
using Microsoft.EntityFrameworkCore;

namespace LaCasitaDeMiga.Features.Orders.Services {
    public class OrderServiceImpl : IOrderService {
        private readonly ApplicationDbContext _context;
        private readonly IProductService _productService;
        private readonly IMapper _mapper;

        public OrderServiceImpl(ApplicationDbContext context, IProductService productService, IMapper mapper) {
            _context = context;
            _productService = productService;
            _mapper = mapper;
        }

        // 1. EL CHECKOUT (CREAR ORDEN CON TRANSACCIÓN)
        public async Task<OrderResponseDto> CreateOrderAsync(OrderRequestDto request) {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try {
                // Primero validamos si el usuario/cliente realmente existe en la BD
                // para evitar que creen órdenes con un Guid falso que rompa la FK de Postgres.
                var userExists = await _context.Users.AnyAsync(u => u.Id == request.CustomerId);
                if (!userExists) {
                    throw new NotFoundException($"El cliente con ID {request.CustomerId} no está registrado.");
                }

                var order = new OrderEntity {
                    CustomerId = request.CustomerId,
                    Status = EOrderStatus.Pending,
                    TotalAmount = 0
                };

                decimal totalAccumulated = 0;

                foreach (var itemDto in request.Items) {
                    if (itemDto.Quantity <= 0) {
                        throw new BadRequestException($"Cantidad inválida ({itemDto.Quantity}) para la variante {itemDto.ProductVariantId}. Debe ser mayor a 0.");
                    }

                    var variant = await _context.ProductVariants
                        .Include(v => v.Product)
                        .FirstOrDefaultAsync(v => v.Id == itemDto.ProductVariantId);

                    if (variant == null) {
                        throw new NotFoundException($"La variante de producto con ID {itemDto.ProductVariantId} no existe.");
                    }

                    bool stockUpdated = await _productService.UpdateStockAsync(variant.Id, -itemDto.Quantity);

                    if (!stockUpdated) {
                        throw new BadRequestException($"Stock insuficiente para el producto: {variant.Product?.Name ?? "Desconocido"} (SKU: {variant.Sku}). Disponibles: {variant.Stock}");
                    }

                    var orderItem = new OrderItemEntity {
                        ProductVariantId = variant.Id,
                        Quantity = itemDto.Quantity,
                        UnitPrice = variant.Price
                    };

                    totalAccumulated += orderItem.Quantity * orderItem.UnitPrice;
                    order.Items.Add(orderItem);
                }

                order.TotalAmount = totalAccumulated;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // MODIFICACIÓN AQUÍ: Agregamos .Include(o => o.Customer) para hidratar el Mapper
                var fullOrder = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.ProductVariant)
                            .ThenInclude(v => v.Product)
                    .FirstAsync(o => o.Id == order.Id);

                return _mapper.Map<OrderResponseDto>(fullOrder);
            } catch (Exception) {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // 2. OBTENER POR ID
        public async Task<OrderResponseDto> GetByIdAsync(Guid id) {
            // MODIFICACIÓN AQUÍ: Agregamos .Include(o => o.Customer)
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                    .ThenInclude(i => i.ProductVariant)
                        .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) {
                throw new NotFoundException($"La orden con ID {id} no fue encontrada.");
            }

            return _mapper.Map<OrderResponseDto>(order);
        }

        // 3. HISTORIAL DEL CLIENTE
        public async Task<IEnumerable<OrderResponseDto>> GetByCustomerIdAsync(Guid customerId) {
            // MODIFICACIÓN AQUÍ: Agregamos .Include(o => o.Customer)
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                    .ThenInclude(i => i.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<OrderResponseDto>>(orders);
        }

        // 4. CAMBIAR ESTADO (PAGADO, ENVIADO, CANCELADO)
        public async Task<OrderResponseDto> UpdateStatusAsync(Guid orderId, EOrderStatus newStatus) {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) {
                throw new NotFoundException($"La orden con ID {orderId} no existe.");
            }

            if (newStatus == EOrderStatus.Cancelled && order.Status != EOrderStatus.Cancelled) {
                foreach (var item in order.Items) {
                    await _productService.UpdateStockAsync(item.ProductVariantId, item.Quantity);
                }
            }

            order.Status = newStatus;
            await _context.SaveChangesAsync();

            return await GetByIdAsync(orderId);
        }
    }
}