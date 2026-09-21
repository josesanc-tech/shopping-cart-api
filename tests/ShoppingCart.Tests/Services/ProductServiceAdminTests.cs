using ShoppingCart.Application.DTOs.Cart;
using ShoppingCart.Application.DTOs.Products;
using ShoppingCart.Application.Exceptions;
using ShoppingCart.Application.Services;
using ShoppingCart.Domain.Entities;
using ShoppingCart.Tests.Helpers;
using Xunit;

namespace ShoppingCart.Tests.Services;

public class ProductServiceAdminTests
{
    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidData_ReturnsProductWithId()
    {
        await using var context = TestDbContextFactory.Create();
        var service = new ProductService(context);

        var request = new CreateProductRequest(
            "NEW01", "Producto Nuevo", "Desc", "Test", 99.99m, 5);

        var result = await service.CreateAsync(request);

        Assert.True(result.Id > 0);
        Assert.Equal("NEW01",          result.Code);
        Assert.Equal("Producto Nuevo", result.Name);
        Assert.Equal(99.99m,           result.Price);
        Assert.Equal(5,                result.Stock);
    }

    [Fact]
    public async Task Create_WithDuplicateCode_ThrowsConflict()
    {
        await using var context = TestDbContextFactory.Create();
        var (_, cheap, _, _) = await TestDataSeeder.SeedAsync(context);
        var service = new ProductService(context);

        var request = new CreateProductRequest(
            cheap.Code, "Otro", "Desc", "Test", 10m, 1);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(request));
    }

    [Theory]
    [InlineData("",      "Name",  10,   1)]   // código vacío
    [InlineData("CODE1", "",      10,   1)]   // nombre vacío
    [InlineData("CODE2", "Name", -1,   1)]   // precio negativo
    [InlineData("CODE3", "Name",  10,  -1)]  // stock negativo
    public async Task Create_WithInvalidData_ThrowsBadRequest(
        string code, string name, decimal price, int stock)
    {
        await using var context = TestDbContextFactory.Create();
        var service = new ProductService(context);

        var request = new CreateProductRequest(code, name, "Desc", "Cat", price, stock);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(request));
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithValidData_UpdatesFields()
    {
        await using var context = TestDbContextFactory.Create();
        var (_, cheap, _, _) = await TestDataSeeder.SeedAsync(context);
        var service = new ProductService(context);

        var request = new UpdateProductRequest(
            "Nombre Actualizado", "Nueva Desc", "Nueva Cat", 55m, 7);

        var result = await service.UpdateAsync(cheap.Id, request);

        Assert.Equal("Nombre Actualizado", result.Name);
        Assert.Equal(55m,                  result.Price);
        Assert.Equal(7,                    result.Stock);
        Assert.Equal(cheap.Code,           result.Code); // Code no debe cambiar
    }

    [Fact]
    public async Task Update_WithNonExistentId_ThrowsNotFound()
    {
        await using var context = TestDbContextFactory.Create();
        var service = new ProductService(context);

        var request = new UpdateProductRequest("N", "D", "C", 10m, 1);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(999, request));
    }

    [Theory]
    [InlineData("",    "D", "C", 10,  1)]  // nombre vacío
    [InlineData("Name","D", "C", -1,  1)]  // precio negativo
    [InlineData("Name","D", "C", 10, -1)]  // stock negativo
    public async Task Update_WithInvalidData_ThrowsBadRequest(
        string name, string desc, string cat, decimal price, int stock)
    {
        await using var context = TestDbContextFactory.Create();
        var (_, cheap, _, _) = await TestDataSeeder.SeedAsync(context);
        var service = new ProductService(context);

        var request = new UpdateProductRequest(name, desc, cat, price, stock);

        await Assert.ThrowsAsync<BadRequestException>(() => service.UpdateAsync(cheap.Id, request));
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenNotReferenced_RemovesProduct()
    {
        await using var context = TestDbContextFactory.Create();
        var (_, cheap, _, _) = await TestDataSeeder.SeedAsync(context);
        var service = new ProductService(context);

        await service.DeleteAsync(cheap.Id);

        var product = await context.Products.FindAsync(cheap.Id);
        Assert.Null(product);
    }

    [Fact]
    public async Task Delete_WhenProductInCart_RemovesCartItemsAndProduct()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, cheap, _, _) = await TestDataSeeder.SeedAsync(context);
        var cartService    = new CartService(context);
        var productService = new ProductService(context);

        await cartService.AddItemAsync(user.Id,
            new AddCartItemRequest(cheap.Id, 2));

        await productService.DeleteAsync(cheap.Id);

        Assert.Null(await context.Products.FindAsync(cheap.Id));
        Assert.Empty(context.CartItems.Where(ci => ci.ProductId == cheap.Id));
    }

    [Fact]
    public async Task Delete_WhenProductHasOrders_ThrowsConflict()
    {
        await using var context = TestDbContextFactory.Create();
        var (user, cheap, _, _) = await TestDataSeeder.SeedAsync(context);

        // Crear una orden con el producto manualmente (snapshot de historial)
        var order = new Order
        {
            OrderNumber = "ORD-TEST",
            UserId      = user.Id,
            Subtotal    = 30m,
            TotalAmount = 30m,
            Items       = new List<OrderItem>
            {
                new()
                {
                    ProductId   = cheap.Id,
                    ProductName = cheap.Name,
                    Quantity    = 1,
                    UnitPrice   = 30m,
                    Subtotal    = 30m
                }
            }
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var service = new ProductService(context);

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(cheap.Id));
    }

    [Fact]
    public async Task Delete_WithNonExistentId_ThrowsNotFound()
    {
        await using var context = TestDbContextFactory.Create();
        var service = new ProductService(context);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(999));
    }
}
