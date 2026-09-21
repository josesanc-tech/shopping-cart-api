using Microsoft.EntityFrameworkCore;
using ShoppingCart.Domain.Entities;

namespace ShoppingCart.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Users.AnyAsync()) return;

        var customer = new User
        {
            Username = "cliente",
            Email = "cliente@demo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cliente123!"),
            Role = Roles.Customer
        };

        var admin = new User
        {
            Username = "admin",
            Email = "admin@demo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = Roles.Admin
        };

        context.Users.AddRange(customer, admin);

        context.Products.AddRange(
            new Product { Code = "P001", Name = "Laptop Pro 15\"", Description = "Laptop profesional 16GB RAM, 512GB SSD", Category = "Electrónica", Price = 1200m, Stock = 10 },
            new Product { Code = "P002", Name = "Mouse Inalámbrico", Description = "Mouse ergonómico con receptor USB", Category = "Accesorios", Price = 25m, Stock = 50 },
            new Product { Code = "P003", Name = "Teclado Mecánico RGB", Description = "Teclado mecánico switches azules", Category = "Accesorios", Price = 80m, Stock = 30 },
            new Product { Code = "P004", Name = "Monitor 27\" 4K", Description = "Monitor IPS 4K UHD 60Hz", Category = "Electrónica", Price = 350m, Stock = 15 },
            new Product { Code = "P005", Name = "Webcam HD 1080p", Description = "Cámara web con micrófono integrado", Category = "Accesorios", Price = 45m, Stock = 0 },
            new Product { Code = "P006", Name = "Auriculares Bluetooth", Description = "Auriculares over-ear con cancelación de ruido", Category = "Audio", Price = 120m, Stock = 20 }
        );

        await context.SaveChangesAsync();
    }
}