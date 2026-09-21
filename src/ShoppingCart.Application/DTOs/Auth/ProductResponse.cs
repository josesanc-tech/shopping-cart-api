

namespace ShoppingCart.Application.DTOs.Auth;
    public record ProductResponse(
    int Id,
    string Code,
    string Name,
    string Description,
    string Category,
    decimal Price,
    int Stock);

