namespace ShoppingCart.Application.DTOs.Products;

public record UpdateProductRequest(
    string Name,
    string Description,
    string Category,
    decimal Price,
    int Stock);
