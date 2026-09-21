using System;
using System.Collections.Generic;
using System.Text;

namespace ShoppingCart.Application.DTOs.Cart;
public record CartItemResponse(
    int ProductId,
    string ProductCode,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal,
    int AvailableStock);