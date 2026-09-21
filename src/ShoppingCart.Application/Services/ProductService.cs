using Microsoft.EntityFrameworkCore;
using ShoppingCart.Application.Common;
using ShoppingCart.Application.DTOs.Products;
using ShoppingCart.Application.Exceptions;
using ShoppingCart.Domain.Entities;

namespace ShoppingCart.Application.Services;

public class ProductService
{
    private readonly IApplicationDbContext _context;

    public ProductService(IApplicationDbContext context) => _context = context;

    // ── Lectura ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<ProductResponse>> GetAllAsync(
        string? search = null,
        string? category = null)
    {
        var query = _context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Code.ToLower().Contains(term) ||
                p.Category.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLower();
            query = query.Where(p => p.Category.ToLower() == cat);
        }

        return await query
            .OrderBy(p => p.Name)
            .Select(p => MapToResponse(p))
            .ToListAsync();
    }

    public async Task<ProductResponse> GetByIdAsync(int id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Producto con Id {id} no encontrado.");

        return MapToResponse(product);
    }

    // ── Admin: Escritura ─────────────────────────────────────────────────────

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new BadRequestException("El código es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BadRequestException("El nombre es obligatorio.");
        if (request.Price < 0)
            throw new BadRequestException("El precio no puede ser negativo.");
        if (request.Stock < 0)
            throw new BadRequestException("El stock no puede ser negativo.");

        var codeExists = await _context.Products
            .AnyAsync(p => p.Code == request.Code.Trim());
        if (codeExists)
            throw new ConflictException(
                $"Ya existe un producto con el código '{request.Code}'.");

        var product = new Product
        {
            Code        = request.Code.Trim(),
            Name        = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Category    = request.Category?.Trim()    ?? string.Empty,
            Price       = request.Price,
            Stock       = request.Stock
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return MapToResponse(product);
    }

    public async Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BadRequestException("El nombre es obligatorio.");
        if (request.Price < 0)
            throw new BadRequestException("El precio no puede ser negativo.");
        if (request.Stock < 0)
            throw new BadRequestException("El stock no puede ser negativo.");

        var product = await _context.Products.FindAsync(id)
            ?? throw new NotFoundException($"Producto con Id {id} no encontrado.");

        product.Name        = request.Name.Trim();
        product.Description = request.Description?.Trim() ?? string.Empty;
        product.Category    = request.Category?.Trim()    ?? string.Empty;
        product.Price       = request.Price;
        product.Stock       = request.Stock;

        await _context.SaveChangesAsync(); // RowVersion protege contra concurrencia

        return MapToResponse(product);
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id)
            ?? throw new NotFoundException($"Producto con Id {id} no encontrado.");

        // Bloquear borrado si el producto tiene órdenes (historial inmutable)
        var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
        if (hasOrders)
            throw new ConflictException(
                "No se puede eliminar el producto: tiene órdenes asociadas. " +
                "Considere desactivarlo en su lugar.");

        // Limpiar items de carrito que referencian el producto
        var cartItems = await _context.CartItems
            .Where(ci => ci.ProductId == id)
            .ToListAsync();

        if (cartItems.Count > 0)
            _context.CartItems.RemoveRange(cartItems);

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ProductResponse MapToResponse(Product p)
        => new(p.Id, p.Code, p.Name, p.Description, p.Category, p.Price, p.Stock);
}
