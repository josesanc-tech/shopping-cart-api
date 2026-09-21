namespace ShoppingCart.Application.DTOs.Orders;

public record OrderResponse(
    int Id,
    string OrderNumber,
    DateTime OrderDate,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TotalAmount,
    IEnumerable<OrderItemResponse> Items);