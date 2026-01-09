using Demo.App.Models;
using Microsoft.EntityFrameworkCore;

namespace Demo.App.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed data
        modelBuilder.Entity<Customer>().HasData(
            new Customer { Id = 1, Name = "Jan Jansen", Email = "jan.jansen@example.com" },
            new Customer { Id = 2, Name = "Piet Pietersen", Email = "piet.pietersen@example.com" }
        );

        modelBuilder.Entity<Book>().HasData(
            new Book { Id = 1, Title = "C# in Depth", Author = "Jon Skeet", Price = 49.99m },
            new Book { Id = 2, Title = "Clean Code", Author = "Robert C. Martin", Price = 39.99m },
            new Book { Id = 3, Title = "The Pragmatic Programmer", Author = "Andrew Hunt", Price = 44.99m }
        );
    }
}