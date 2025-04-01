using Microsoft.EntityFrameworkCore;
using OrderManagementAPI.Core.Models;

namespace OrderManagementAPI.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Seed some example products
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Laptop", Price = 1299.99m, StockQuantity = 10, Description = "High performance laptop" },
                new Product { Id = 2, Name = "Smartphone", Price = 899.99m, StockQuantity = 15, Description = "Latest smartphone model" },
                new Product { Id = 3, Name = "Headphones", Price = 199.99m, StockQuantity = 20, Description = "Wireless noise-cancelling headphones" }
            );
        }
    }
} 