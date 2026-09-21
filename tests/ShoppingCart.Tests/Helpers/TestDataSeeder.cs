using ShoppingCart.Domain.Constants;
using ShoppingCart.Domain.Entities;
using ShoppingCart.Infrastructure.Data;

namespace ShoppingCart.Tests.Helpers;

public static class TestDataSeeder
{
    public static async Task<(User user, Product cheapProduct, Product expensiveProduct, Product outOfStock)> SeedAsync(
        AppDbContext context)
    {
        var user = new User
        {
            Username = "testuser",
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = Roles.Customer
        };

        var cheapProduct = new Product
        {
            Code = "CHEAP01",
            Name = "Producto Barato",
            Category = "Test",
            Price = 30m,
            Stock = 10
        };

        var expensiveProduct = new Product
        {
            Code = "EXP01",
            Name = "Producto Caro",
            Category = "Test",
            Price = 80m,
            Stock = 10
        };

        var outOfStock = new Product
        {
            Code = "OOS01",
            Name = "Agotado",
            Category = "Test",
            Price = 50m,
            Stock = 0
        };

        context.Users.Add(user);
        context.Products.AddRange(cheapProduct, expensiveProduct, outOfStock);
        await context.SaveChangesAsync();

        return (user, cheapProduct, expensiveProduct, outOfStock);
    }
}