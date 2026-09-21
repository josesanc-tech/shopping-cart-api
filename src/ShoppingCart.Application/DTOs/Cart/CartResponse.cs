
namespace ShoppingCart.Application.DTOs.Cart;
public record CartResponse(
    IEnumerable<CartItemResponse> Items,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TotalAmount);
