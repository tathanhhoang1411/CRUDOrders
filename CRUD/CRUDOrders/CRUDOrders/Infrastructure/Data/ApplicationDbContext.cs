using Microsoft.EntityFrameworkCore;
using CRUDOrders.Core.Entities;

namespace CRUDOrders.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(e => e.Code).IsUnique();
            // Optimistic concurrency token — SQL Server ROWVERSION
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(20).HasDefaultValue("Staff");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Pending");
            entity.Property(e => e.Total).HasPrecision(18, 2);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.HasMany(e => e.OrderItems).WithOne().HasForeignKey("OrderId");
            entity.HasIndex(e => e.UserId);
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ProductId);
        });
    }
}
