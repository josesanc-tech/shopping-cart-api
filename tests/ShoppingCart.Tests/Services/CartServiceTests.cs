using ShoppingCart.Application.DTOs.Cart;
using ShoppingCart.Application.Exceptions;
using ShoppingCart.Application.Services;
using ShoppingCart.Tests.Helpers;

namespace ShoppingCart.Tests.Services;

public class CartServiceTests
{

    [Fact]
    public async Task GetCart_WhenUserHasNoCart_ReturnsEmptyCart()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, _, _, _) = await TestDataSeeder.SeedAsync(context);
        var service = new CartService(context);

        var result = await service.GetCartAsync(user.Id);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0m, result.Subtotal);
        Assert.Equal(0m, result.DiscountAmount);
        Assert.Equal(0m, result.TotalAmount);
    }


    [Fact]
    public async Task AddItem_WhenQuantityIsZeroOrNegative_ThrowsBadRequest()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, cheap, _, _) = await TestDataSeeder.SeedAsync(context);
        var service = new CartService(context);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.AddItemAsync(user.Id, new AddCartItemRequest(cheap.Id, 0)));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.AddItemAsync(user.Id, new AddCartItemRequest(cheap.Id, -1)));
    }

    [Fact]
    public async Task AddItem_WhenProductDoesNotExist_ThrowsNotFound()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, _, _, _) = await TestDataSeeder.SeedAsync(context);
        var service = new CartService(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.AddItemAsync(user.Id, new AddCartItemRequest(9999, 1)));
    }

    [Fact]
    public async Task AddItem_WhenQuantityExceedsStock_ThrowsInsufficientStock()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, cheap, _, _) = await TestDataSeeder.SeedAsync(context);
        var service = new CartService(context);

        // stock 10, pedimos 11
        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            service.AddItemAsync(user.Id, new AddCartItemRequest(cheap.Id, 11)));
    }

    [Fact]
    public async Task AddItem_WhenAccumulatedQuantityExceedsStock_ThrowsInsufficientStock()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, cheap, _, _) = await TestDataSeeder.SeedAsync(context);
        var service = new CartService(context);

        // Primer add: 6 unidades
        await service.AddItemAsync(user.Id, new AddCartItemRequest(cheap.Id, 6));

        // Segundo add: 6 más (total 12) stock 10 deberia fallar
        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            service.AddItemAsync(user.Id, new AddCartItemRequest(cheap.Id, 6)));
    }

    [Fact]
    public async Task AddItem_WhenCalledTwice_MergesQuantityInSameItem()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, cheap, _, _) = await TestDataSeeder.SeedAsync(context);
        var service = new CartService(context);

        await service.AddItemAsync(user.Id, new AddCartItemRequest(cheap.Id, 2));
        var result = await service.AddItemAsync(user.Id, new AddCartItemRequest(cheap.Id, 3));

        Assert.Single(result.Items);
        Assert.Equal(5, result.Items.First().Quantity);
    }

    // ---------- Descuento

    [Fact]
    public async Task Cart_WhenSubtotalIsExactly100_DoesNotApplyDiscount()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, _, _, _) = await TestDataSeeder.SeedAsync(context);
        var service = new CartService(context);

        // Agregamos un producto a $100 exacto
        context.Products.Add(new Domain.Entities.Product
        {
            Code = "EXACT",
            Name = "Exacto100",
            Category = "Test",
            Price = 100m,
            Stock = 5
        });
        await context.SaveChangesAsync();

        var product = context.Products.First(p => p.Code == "EXACT");
        var result = await service.AddItemAsync(user.Id, new AddCartItemRequest(product.Id, 1));

        Assert.Equal(100m, result.Subtotal);
        Assert.Equal(0m, result.DiscountAmount);
        Assert.Equal(100m, result.TotalAmount);
    }

    [Fact]
    public async Task Cart_WhenSubtotalExceeds100_Applies10PercentDiscount()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, _, expensive, _) = await TestDataSeeder.SeedAsync(context);
        var service = new CartService(context);

        var result = await service.AddItemAsync(user.Id, new AddCartItemRequest(expensive.Id, 2));

        Assert.Equal(160m, result.Subtotal);
        Assert.Equal(16m, result.DiscountAmount);
        Assert.Equal(144m, result.TotalAmount);
    }

    // ---------- Actualizacion del objeto

    [Fact]
    public async Task UpdateItem_WhenQuantityExceedsStock_ThrowsInsufficientStock()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, cheap, _, _) = await TestDataSeeder.SeedAsync(context);
        var service = new CartService(context);

        await service.AddItemAsync(user.Id, new AddCartItemRequest(cheap.Id, 1));

        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            service.UpdateItemAsync(user.Id, cheap.Id, 99));
    }

    [Fact]
    public async Task UpdateItem_WhenProductNotInCart_ThrowsNotFound()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, cheap, _, _) = await TestDataSeeder.SeedAsync(context);
        var service = new CartService(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateItemAsync(user.Id, cheap.Id, 1));
    }

    // ---------- Se remueve un objeto del carrito

    [Fact]
    public async Task RemoveItem_WhenProductInCart_RemovesSuccessfully()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, cheap, _, _) = await TestDataSeeder.SeedAsync(context);
        var service = new CartService(context);

        await service.AddItemAsync(user.Id, new AddCartItemRequest(cheap.Id, 1));
        await service.RemoveItemAsync(user.Id, cheap.Id);

        var result = await service.GetCartAsync(user.Id);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Clear_RemovesAllItems()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, cheap, expensive, _) = await TestDataSeeder.SeedAsync(context);
        var service = new CartService(context);

        await service.AddItemAsync(user.Id, new AddCartItemRequest(cheap.Id, 1));
        await service.AddItemAsync(user.Id, new AddCartItemRequest(expensive.Id, 1));
        await service.ClearAsync(user.Id);

        var result = await service.GetCartAsync(user.Id);
        Assert.Empty(result.Items);
    }
}