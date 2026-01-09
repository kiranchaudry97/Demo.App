using Demo.SharedModels.Models;
using Microsoft.EntityFrameworkCore;

namespace Demo.MauiApp.Data;

public class AppDbContext : DbContext
{
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}
