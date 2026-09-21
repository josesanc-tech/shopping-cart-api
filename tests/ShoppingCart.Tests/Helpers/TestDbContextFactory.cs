using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ShoppingCart.Infrastructure.Data;

namespace ShoppingCart.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        // SQLite en memoria
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}