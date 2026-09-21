
using Microsoft.EntityFrameworkCore;
using ShoppingCart.Domain.Entities;

namespace ShoppingCart.Application.Common;

    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Product> Products { get; }
        DbSet<Cart> Carts { get; }
        DbSet<CartItem> CartItems { get; }
        DbSet<Order> Orders { get; }
        DbSet<OrderItem> OrderItems { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken = default);

         Task<int> TryDecreaseProductStockAsync(
            int productId,
            int quantity,
            CancellationToken cancellationToken = default);
}

