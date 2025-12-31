using Microsoft.EntityFrameworkCore; // <--- ACEASTA LINIE LIPSEȘTE PROBABIL
using DemoApi.Models.Entities;

namespace DemoApi.Data;

// Moștenim din DbContext
public class AppDbContext : DbContext
{
    // Constructorul primește opțiunile (connection string, provider) și le dă la părinte
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Aici definim tabelele. Numele proprietății = Numele tabelului în SQL.
    public DbSet<Product> Products { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<Order> Orders {get; set;}

    public DbSet<OrderItem> OrderItems {get; set;}

    public DbSet<Address> Addresses { get; set; }
}