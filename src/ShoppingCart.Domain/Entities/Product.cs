using System;
using System.Collections.Generic;
using System.Text;

namespace ShoppingCart.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
