using AutoMapper;
using LaCasitaDeMiga.Common.DTOs;
using LaCasitaDeMiga.Data;
using LaCasitaDeMiga.Exceptions;
using LaCasitaDeMiga.Features.Common.Cache.services;
using LaCasitaDeMiga.Features.Common.services.MailService;
using LaCasitaDeMiga.Features.Common.services.MailService.Enums;
using LaCasitaDeMiga.Features.Orders.DTOs;
using LaCasitaDeMiga.Features.Products.DTOs;
using LaCasitaDeMiga.Features.Products.Services;
using LaCasitaDeMiga.Features.Users.services;
using Microsoft.EntityFrameworkCore;

namespace LaCasitaDeMiga.Features.Orders.Services {
    public class OrderServiceImpl : IOrderService {
        private readonly ApplicationDbContext _context;

        private readonly IProductService _productService;
        private readonly IEmailTemplateService _emailService;
        private readonly IMapper _mapper;

     
        public OrderServiceImpl(ApplicationDbContext context, IProductService productService,
                                IMapper mapper, IUserService userService, IEmailTemplateService emailService) {
            _context = context;
            _productService = productService;
            _mapper = mapper;
            _emailService = emailService;


        }


        // 1. EL CHECKOUT (CREAR ORDEN CON TRANSACCIÓN Y CONGELACIÓN DE COSTOS)
        public async Task<OrderResponseDto> CreateOrderAsync(OrderRequestDto request) {
            // Iniciamos la transacción ACID en PostgreSQL para asegurar consistencia
            using var transaction = await _context.Database.BeginTransactionAsync();

            try {
                var order = new OrderEntity {
                    CustomerId = request.CustomerId,
                    Status = EOrderStatus.Pending,
                    TotalAmount = 0 // Lo calcularemos fila por fila en el bucle
                };

                decimal totalAccumulated = 0;

                foreach (var itemDto in request.Items) {

                    if (itemDto.Quantity <= 0) {
                        throw new BadRequestException($"Cantidad inválida ({itemDto.Quantity}) para la variante {itemDto.ProductVariantId}. Debe ser mayor a 0.");
                    }

                    // Buscamos la variante directo en la BD para validar existencia y obtener los valores financieros reales
                    var variant = await _context.ProductVariants
                        .Include(v => v.Product)
                        .FirstOrDefaultAsync(v => v.Id == itemDto.ProductVariantId);

                    if (variant == null) {
                        throw new NotFoundException($"La variante de producto con ID {itemDto.ProductVariantId} no existe.");
                    }

                    // Intentamos restar el stock usando tu servicio de productos.
                    // Le pasamos la cantidad en NEGATIVO para que reste.
                    bool stockUpdated = await _productService.UpdateStockAsync(variant.Id, -itemDto.Quantity);

                    if (!stockUpdated) {
                        throw new BadRequestException($"Stock insuficiente para el producto: {variant.Product?.Name ?? "Desconocido"} (SKU: {variant.Sku}). Disponibles: {variant.Stock}");
                    }

                    // Creamos el ítem histórico de la orden guardando la foto económica del momento
                    var orderItem = new OrderItemEntity {
                        ProductVariantId = variant.Id,
                        Quantity = itemDto.Quantity,
                        UnitPrice = variant.Price,      // Congelamos el precio de venta cobrado al público
                        UnitCost = variant.AverageCost   // ◄ ¡CLAVE FINANCIERA! Congelamos el costo promedio ponderado actual
                    };
                    totalAccumulated += orderItem.Quantity * orderItem.UnitPrice;
                    order.Items.Add(orderItem);
                }

                order.TotalAmount = totalAccumulated;

                // Guardamos la cabecera y los detalles en la base de datos
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Si todo llegó hasta acá sin excepciones, consolidamos los cambios físicamente en PostgreSQL
                await transaction.CommitAsync();

                // Rehidratamos la entidad con todos sus Includes para que el AutoMapper pueda armar el VariantName dinámico
                var fullOrder = await _context.Orders
                    .Include(o => o.Items)
                        .ThenInclude(i => i.ProductVariant)
                            .ThenInclude(v => v.Product)
                    .FirstAsync(o => o.Id == order.Id);

                return _mapper.Map<OrderResponseDto>(fullOrder);
            } catch (Exception) {
                // Si algo falló (Ej: BadRequestException por falta de stock), deshacemos absolutamente todo
                await transaction.RollbackAsync();
                throw; // Re-lanzamos la excepción para que el GlobalExceptionHandler devuelva el código correcto
            }
        }

        //borrar para github recluter
        public async Task<OrderResponseDto> CreateOrderAsync(ComboEspecialDTO request) {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try {
                var order = new OrderEntity {
                    CustomerId = request.CustomerId,
                    Status = EOrderStatus.Pending,
                    TotalAmount = request.TotalAmount,
                    CreatedAt = DateTime.UtcNow // ◄ ¡ERROR AQUÍ!
                };

                // ID genérico para productos no registrados que creaste en tu BD
                Guid variantEspecialGenericaId = Guid.Parse("99999999-9999-9999-9999-999999999999");

                // 1. Agregar el detalle de los sándwiches comunes (si hay)
                if (request.CantComunes > 0) {
                    order.Items.Add(new OrderItemEntity {
                        ProductVariantId = variantEspecialGenericaId,
                        Quantity = request.CantComunes,
                        UnitPrice = request.PriceComunes,
                        UnitCost = 0 // Al no estar registrado, el costo es 0 o lo que estimes
                                     // Si tu OrderItem tiene un campo 'Notes' o 'Description', podrías guardar: "Sándwiches Comunes Manuales"
                    });
                }

                // 2. Agregar el detalle de los sándwiches especiales (si hay)
                if (request.CantEspeciales > 0) {
                    order.Items.Add(new OrderItemEntity {
                        ProductVariantId = variantEspecialGenericaId,
                        Quantity = request.CantEspeciales,
                        UnitPrice = request.PriceEspeciales,
                        UnitCost = 0
                    });
                }

                // Guardamos todo junto (Cabecera + Detalles Genéricos)
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Rehidratamos para que el AutoMapper no falle buscando las relaciones
                var fullOrder = await _context.Orders
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
        //---------------------------------------------------------------------------
        public async Task<PagedResultDto<OrderResponseDto>> GetAllAsync(
    EOrderStatus? status = null,
    DateTime? startDate = null,
    DateTime? endDate = null,
    int pageNumber = 1,
    int pageSize = 10) {

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var query = _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.ProductVariant)
                        .ThenInclude(v => v.Product)
                .AsQueryable();

            if (status.HasValue) {
                query = query.Where(o => o.Status == status.Value);
            }

            if (startDate.HasValue) {
                query = query.Where(o => o.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue) {
                query = query.Where(o => o.CreatedAt <= endDate.Value);
            }

            var totalItems = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var mappedItems = _mapper.Map<IEnumerable<OrderResponseDto>>(orders);

            return new PagedResultDto<OrderResponseDto> {
                Items = mappedItems,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        //-----------------------------------------------------------------------------








        // 2. OBTENER POR ID
        public async Task<OrderResponseDto> GetByIdAsync(Guid id) {
            var order = await _context.Orders
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
            var orders = await _context.Orders
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

            // Lógica comercial: Si se cancela una orden que estaba activa, le devolvemos el stock al depósito
            if (newStatus == EOrderStatus.Cancelled && order.Status != EOrderStatus.Cancelled) {
                foreach (var item in order.Items) {
                    // Pasamos la cantidad en POSITIVO para que sume stock nuevamente
                    await _productService.UpdateStockAsync(item.ProductVariantId, item.Quantity);
                }
            }

            order.Status = newStatus;
            await _context.SaveChangesAsync();

            return await GetByIdAsync(orderId);
        }

        public async Task SendOrderConfirmationEmailAsync(OrderResponseDto order) {
            // 1. Buscamos si el usuario existe en Neon
            var user = await _context.Users.FindAsync(order.CustomerId);

            // Por seguridad, si el usuario no existe, no le avisamos al front (evita que husmeen emails válidos)
            if (user == null) throw new NotFoundException($"El usuario con ID {order.CustomerId} no fue encontrado.");

            var emailParams = new {

                ORDER_ID = order.Id.ToString(),
                ORDER_DATE = order.CreatedAt.ToString("dd/MM/yyyy"),
                ITEMS = order.Items,
                TOTAL = order.TotalAmount,

                USER_NAME = user.Name
            };

            await _emailService.SendTemplateEmailAsync(user.Email, EEmailTemplate.SEND_ORDER_CONFIRMATION, emailParams);
        }


        //borrar para github recluter
        public async Task SendOrderConfirmationEmailAsync(ComboEspecialDTO order) {
            var user = await _context.Users.FindAsync(order.CustomerId);

            if (user == null) throw new NotFoundException($"El usuario con ID {order.CustomerId} no fue encontrado.");

            var emailParams = new {

                ORDER_ID = order.Id.ToString(),
                ORDER_DATE = order.CreatedAt.ToString("dd/MM/yyyy"),
                CANT_COMUNES = order.CantComunes,
                PRICE_COMUNES = order.PriceComunes,
                SUBTOTAL_COMUNES= order.SubTotalComunes,

                CANT_ESPECIALES = order.CantEspeciales,
                PRICE_ESPECIALES = order.PriceEspeciales,
                SUBTOTAL_ESPECIALES= order.SubTotalEspeciales,

                TOTAL = order.TotalAmount,

                USER_NAME = user.Name
            };
            await _emailService.SendTemplateEmailAsync(user.Email, EEmailTemplate.SEND_ORDER_ESPECIAL_CONFIRMATION, emailParams);

        }
    }
}