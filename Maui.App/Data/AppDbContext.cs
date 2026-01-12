#if USE_EF
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using Maui.App.Models;

namespace Maui.App.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var converter = new ValueConverter<List<int>, string>(
            v => JsonSerializer.Serialize<List<int>>(v),
            v => JsonSerializer.Deserialize<List<int>>(v) ?? new List<int>());

        modelBuilder.Entity<Order>().Property(o => o.BookIds).HasConversion(converter);
    }
}
#endif
