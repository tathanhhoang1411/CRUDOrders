using CRUDOrders.Core.Entities;
using CRUDOrders.Infrastructure.Services;
using System.Security.Cryptography;
using System.Text;

namespace CRUDOrders.Infrastructure.Data;

public interface IDbInitializer
{
    Task InitializeAsync();
}

public class DbInitializer : IDbInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly IIdGenerator _idGenerator;

    public DbInitializer(ApplicationDbContext context, IIdGenerator idGenerator)
    {
        _context = context;
        _idGenerator = idGenerator;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Seed admin user and sample data
            if (!_context.Users.Any())
            {
                var adminUser = new User
                {
                    Id = _idGenerator.NewId(),
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = HashPassword("admin123"),
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddHours(7)
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();
            }

            if (!_context.Products.Any())
            {
                var products = new[]
                {
                    new Product
                    {
                        Id = _idGenerator.NewId(),
                        Code = "PROD001",
                        Name = "Laptop",
                        Description = "High-performance laptop",
                        Price = 1500.00m,
                        Stock = 10,
                        CreatedAt = DateTime.UtcNow.AddHours(7)
                    },
                    new Product
                    {
                        Id = _idGenerator.NewId(),
                        Code = "PROD002",
                        Name = "Mouse",
                        Description = "Wireless mouse",
                        Price = 25.00m,
                        Stock = 100,
                        CreatedAt = DateTime.UtcNow.AddHours(7)
                    },
                    new Product
                    {
                        Id = _idGenerator.NewId(),
                        Code = "PROD003",
                        Name = "Keyboard",
                        Description = "Mechanical keyboard",
                        Price = 75.00m,
                        Stock = 50,
                        CreatedAt = DateTime.UtcNow.AddHours(7)
                    }
                };

                _context.Products.AddRange(products);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to initialize database", ex);
        }
    }

    private static string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
