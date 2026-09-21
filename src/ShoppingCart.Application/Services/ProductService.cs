using Microsoft.EntityFrameworkCore;
using ShoppingCart.Application.Common;
using ShoppingCart.Application.DTOs.Products;
using ShoppingCart.Application.Exceptions;

namespace ShoppingCart.Application.Services;

public class ProductService
{
    private readonly IApplicationDbContext _context;

    public ProductService(IApplicationDbContext context) => _context = context;

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
            .Select(p => new ProductResponse(
                p.Id, p.Code, p.Name, p.Description, p.Category, p.Price, p.Stock))
            .ToListAsync();
    }

    public async Task<ProductResponse> GetByIdAsync(int id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Producto con Id {id} no encontrado.");

        return new ProductResponse(
            product.Id, product.Code, product.Name,
            product.Description, product.Category,
            product.Price, product.Stock);
    }
}