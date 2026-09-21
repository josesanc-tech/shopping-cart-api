using Microsoft.EntityFrameworkCore;
using ShoppingCart.Application.Common;
using ShoppingCart.Application.DTOs.Cart;
using ShoppingCart.Application.Exceptions;
using ShoppingCart.Domain.Entities;

namespace ShoppingCart.Application.Services;

public class CartService
{
    private readonly IApplicationDbContext _context;

    private const decimal DiscountThreshold = 100m;
    private const decimal DiscountRate = 0.10m;

    public CartService(IApplicationDbContext context) => _context = context;

    public async Task<CartResponse> GetCartAsync(int userId)
    {
        var cart = await LoadCartAsync(userId);
        return BuildResponse(cart);
    }

    public async Task<CartResponse> AddItemAsync(int userId, AddCartItemRequest request)
    {
        if (request.Quantity <= 0)
            throw new BadRequestException("La cantidad debe ser mayor a cero.");

        var product = await _context.Products.FindAsync(request.ProductId)
            ?? throw new NotFoundException($"Producto {request.ProductId} no encontrado.");

        var cart = await GetOrCreateCartAsync(userId);

        var existing = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        var newQuantity = (existing?.Quantity ?? 0) + request.Quantity;

        if (newQuantity > product.Stock)
            throw new InsufficientStockException(product.Name, product.Stock);

        if (existing is not null)
            existing.Quantity = newQuantity;
        else
            cart.Items.Add(new CartItem { ProductId = request.ProductId, Quantity = request.Quantity });

        await _context.SaveChangesAsync();
        return await GetCartAsync(userId);
    }

    public async Task<CartResponse> UpdateItemAsync(int userId, int productId, int quantity)
    {
        if (quantity <= 0)
            throw new BadRequestException("La cantidad debe ser mayor a cero.");

        var cart = await LoadCartAsync(userId)
            ?? throw new NotFoundException("Carrito no encontrado.");

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new NotFoundException($"El producto {productId} no está en el carrito.");

        var product = await _context.Products.FindAsync(productId)
            ?? throw new NotFoundException($"Producto {productId} no encontrado.");

        if (quantity > product.Stock)
            throw new InsufficientStockException(product.Name, product.Stock);

        item.Quantity = quantity;
        await _context.SaveChangesAsync();
        return await GetCartAsync(userId);
    }

    public async Task RemoveItemAsync(int userId, int productId)
    {
        var cart = await LoadCartAsync(userId)
            ?? throw new NotFoundException("Carrito no encontrado.");

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new NotFoundException($"El producto {productId} no está en el carrito.");

        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();
    }

    public async Task ClearAsync(int userId)
    {
        var cart = await LoadCartAsync(userId);
        if (cart is null || !cart.Items.Any()) return;

        _context.CartItems.RemoveRange(cart.Items);
        await _context.SaveChangesAsync();
    }

    // ------- Helpers
    private async Task<Cart?> LoadCartAsync(int userId)
        => await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

    private async Task<Cart> GetOrCreateCartAsync(int userId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null)
        {
            cart = new Cart { UserId = userId };
            _context.Carts.Add(cart);
        }
        return cart;
    }

    private static CartResponse BuildResponse(Cart cart)
    {
        var items = cart.Items
            .Select(i => new CartItemResponse(
                ProductId: i.ProductId,
                ProductCode: i.Product.Code,
                ProductName: i.Product.Name,
                UnitPrice: i.Product.Price,
                Quantity: i.Quantity,
                Subtotal: i.Product.Price * i.Quantity,
                AvailableStock: i.Product.Stock))
            .ToList();

        var subtotal = items.Sum(i => i.Subtotal);
        var discount = subtotal > DiscountThreshold ? subtotal * DiscountRate : 0m;
        var total = subtotal - discount;

        return new CartResponse(items, subtotal, discount, total);
    }
}