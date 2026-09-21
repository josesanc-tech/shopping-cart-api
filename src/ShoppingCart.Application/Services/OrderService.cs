using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShoppingCart.Application.Common;
using ShoppingCart.Application.DTOs.Orders;
using ShoppingCart.Application.Exceptions;

namespace ShoppingCart.Application.Services;

public class OrderService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<OrderService> _logger;

    private const decimal DiscountThreshold = 100m;
    private const decimal DiscountRate = 0.10m;

    public OrderService(IApplicationDbContext context, ILogger<OrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateOrderAsync(int userId)
    {
        // Cargar carrito
        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null || !cart.Items.Any())
            throw new BadRequestException("El carrito está vacío.");

        // Transaccion 
        await using var transaction = await _context.BeginTransactionAsync();
        try
        {
            foreach (var item in cart.Items)
            {
                var affected = await _context.TryDecreaseProductStockAsync(
                    item.ProductId, item.Quantity);

                if (affected == 0)
                {
                    _logger.LogWarning(
                        "Stock insuficiente para producto {ProductId} al procesar orden de usuario {UserId}",
                        item.ProductId, userId);

                    throw new InsufficientStockException(
                        item.Product.Name, item.Product.Stock);
                }
            }

            var subtotal = cart.Items.Sum(i => i.Product.Price * i.Quantity);
            var discount = subtotal > DiscountThreshold ? subtotal * DiscountRate : 0m;

            var order = new Domain.Entities.Order
            {
                OrderNumber = GenerateOrderNumber(),
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Subtotal = subtotal,
                DiscountAmount = discount,
                TotalAmount = subtotal - discount,
                Items = cart.Items.Select(i => new Domain.Entities.OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.Product.Price,
                    Subtotal = i.Product.Price * i.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);

            // Vaciar carrito
            _context.CartItems.RemoveRange(cart.Items);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Orden {OrderNumber} creada para usuario {UserId} por ${Total}",
                order.OrderNumber, userId, order.TotalAmount);

            return MapToResponse(order);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<OrderResponse>> GetHistoryAsync(int userId)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .Include(o => o.Items)
            .Select(o => new OrderResponse(
                o.Id,
                o.OrderNumber,
                o.OrderDate,
                o.Subtotal,
                o.DiscountAmount,
                o.TotalAmount,
                o.Items.Select(i => new OrderItemResponse(
                    i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.Subtotal))))
            .ToListAsync();
    }

    public async Task<OrderResponse> GetByIdAsync(int userId, int orderId)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId)
            ?? throw new NotFoundException($"Orden {orderId} no encontrada.");

        return MapToResponse(order);
    }

    private static string GenerateOrderNumber()
        => $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

    private static OrderResponse MapToResponse(Domain.Entities.Order order)
        => new(
            order.Id,
            order.OrderNumber,
            order.OrderDate,
            order.Subtotal,
            order.DiscountAmount,
            order.TotalAmount,
            order.Items.Select(i => new OrderItemResponse(
                i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.Subtotal)));
}