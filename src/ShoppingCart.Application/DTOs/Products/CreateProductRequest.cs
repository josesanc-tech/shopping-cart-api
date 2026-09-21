namespace ShoppingCart.Application.DTOs.Products;

public record CreateProductRequest(
    string Code,
    string Name,
    string Description,
    string Category,
    decimal Price,
    int Stock);
