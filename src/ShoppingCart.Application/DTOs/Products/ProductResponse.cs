

namespace ShoppingCart.Application.DTOs.Products;

public record ProductResponse(
    int Id,
    string Code,
    string Name,
    string Description,
    string Category,
    decimal Price,
    int Stock);
