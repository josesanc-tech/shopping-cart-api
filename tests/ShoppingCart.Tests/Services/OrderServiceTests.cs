using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ShoppingCart.Application.Exceptions;
using ShoppingCart.Application.Services;
using ShoppingCart.Domain.Entities;
using ShoppingCart.Tests.Helpers;



namespace ShoppingCart.Tests.Services;

public class OrderServiceTests
{
    private static OrderService CreateService(Infrastructure.Data.AppDbContext context)
        => new(context, NullLogger<OrderService>.Instance);

    [Fact]
    public async Task CreateOrder_WhenCartIsEmpty_ThrowsBadRequest()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, _, _, _) = await TestDataSeeder.SeedAsync(context);
        var service = CreateService(context);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateOrderAsync(user.Id));
    }

    [Fact]
    public async Task CreateOrder_WhenCartHasItems_CreatesOrderAndClearsCart()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, cheap, expensive, _) = await TestDataSeeder.SeedAsync(context);
        var cartService = new CartService(context);
        var orderService = CreateService(context);

        // Carrito: 2 x $80 = $160 → con descuento → $144
        await cartService.AddItemAsync(user.Id, new Application.DTOs.Cart.AddCartItemRequest(expensive.Id, 2));

        var order = await orderService.CreateOrderAsync(user.Id);

        Assert.Equal(160m, order.Subtotal);
        Assert.Equal(16m, order.DiscountAmount);
        Assert.Equal(144m, order.TotalAmount);
        Assert.Single(order.Items);
        Assert.StartsWith("ORD-", order.OrderNumber);

        // Carrito debe quedar vacío
        var cartAfter = await cartService.GetCartAsync(user.Id);
        Assert.Empty(cartAfter.Items);

        // Stock debe haber bajado de 10 a 8
        var product = await context.Products.AsNoTracking().FirstAsync(p => p.Id == expensive.Id);
        Assert.Equal(8, product.Stock);
    }

    [Fact]
    public async Task CreateOrder_WhenStockInsufficient_RollsBackAndThrows()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, cheap, expensive, outOfStock) = await TestDataSeeder.SeedAsync(context);
        var cartService = new CartService(context);
        var orderService = CreateService(context);

        // Agregamos items válidos Y uno que estará agotado al momento de comprar.
        // Como el carrito valida stock al agregar, primero lo agregamos cuando había stock
        // y luego forzamos la situación.
        await cartService.AddItemAsync(user.Id, new Application.DTOs.Cart.AddCartItemRequest(expensive.Id, 2));

        // Reducimos el stock del producto a 1 después de haberlo agregado al carrito
        var product = await context.Products.FindAsync(expensive.Id);
        product!.Stock = 1;
        await context.SaveChangesAsync();

        // Al crear la orden, el UPDATE debe fallar (2 > 1) → rollback
        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            orderService.CreateOrderAsync(user.Id));

        // Verificar rollback: el carrito NO debe haberse vaciado
        var cartAfter = await cartService.GetCartAsync(user.Id);
        Assert.Single(cartAfter.Items);

        // Verificar rollback: no se creó ninguna orden
        var orders = await orderService.GetHistoryAsync(user.Id);
        Assert.Empty(orders);

        // Verificar rollback: el stock del producto sigue en 1 (no se descontó)
        var productAfter = await context.Products.AsNoTracking().FirstAsync(p => p.Id == expensive.Id);
        Assert.Equal(1, productAfter.Stock);
    }

    [Fact]
    public async Task GetHistory_WhenNoOrders_ReturnsEmpty()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, _, _, _) = await TestDataSeeder.SeedAsync(context);
        var service = CreateService(context);

        var history = await service.GetHistoryAsync(user.Id);
        Assert.Empty(history);
    }

    [Fact]
    public async Task GetHistory_ReturnsOnlyUserOrders()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, _, _, _) = await TestDataSeeder.SeedAsync(context);

        var otherUser = new User
        {
            Username = "other",
            Email = "other@test.com",
            PasswordHash = "x",
            Role = Domain.Constants.Roles.Customer
        };
        context.Users.Add(otherUser);
        await context.SaveChangesAsync();

        context.Orders.AddRange(
            new Order { OrderNumber = "ORD-1", UserId = user.Id, Subtotal = 50m, TotalAmount = 50m },
            new Order { OrderNumber = "ORD-2", UserId = user.Id, Subtotal = 100m, TotalAmount = 90m },
            new Order { OrderNumber = "ORD-3", UserId = otherUser.Id, Subtotal = 200m, TotalAmount = 180m }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var history = (await service.GetHistoryAsync(user.Id)).ToList();

        Assert.Equal(2, history.Count);
        Assert.All(history, o => Assert.NotEqual("ORD-3", o.OrderNumber));
    }

    [Fact]
    public async Task GetById_WhenOrderBelongsToOtherUser_ThrowsNotFound()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, _, _, _) = await TestDataSeeder.SeedAsync(context);

        var otherUser = new User
        {
            Username = "other",
            Email = "other@test.com",
            PasswordHash = "x",
            Role = Domain.Constants.Roles.Customer
        };
        context.Users.Add(otherUser);
        await context.SaveChangesAsync();

        var order = new Order { OrderNumber = "ORD-X", UserId = otherUser.Id, TotalAmount = 100m };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // user intenta leer orden de otherUser → debe fallar
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetByIdAsync(user.Id, order.Id));
    }
}