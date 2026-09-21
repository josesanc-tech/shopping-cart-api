using System;
using System.Collections.Generic;
using System.Text;

namespace ShoppingCart.Application.DTOs.Cart;
public record AddCartItemRequest(int ProductId, int Quantity);