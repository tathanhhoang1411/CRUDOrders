using CRUDOrders.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRUDOrders.Infrastructure.Data.Repositories;

public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    /// <summary>Lay entity co change-tracking (dung cho update voi optimistic concurrency).</summary>
    Task<T?> GetByIdTrackedAsync(long id, CancellationToken cancellationToken = default);
    Task<List<T>> GetAllAsync(int skip = 0, int take = 10, string? keyword = null, CancellationToken cancellationToken = default);
    Task<int> CountAsync(string? keyword = null, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default);
}

public class Repository<T> : IRepository<T> where T : Entity
{
    protected readonly ApplicationDbContext Context;
    private readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        Context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
    }

    public async Task<T?> GetByIdTrackedAsync(long id, CancellationToken cancellationToken = default)
    {
        // Co tracking: can thiet de EF Core phat hien DbUpdateConcurrencyException
        return await _dbSet
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
    }

    public async Task<List<T>> GetAllAsync(int skip = 0, int take = 10, string? keyword = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(e => !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = ApplyKeywordFilter(query, keyword);
        }

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(string? keyword = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(e => !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = ApplyKeywordFilter(query, keyword);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = CRUDOrders.Application.Common.DateTimeHelper.UtcPlus7Now;
        _dbSet.Update(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        // Dung tracked entity de EF Core khong bi loi concurrency token khi soft-delete
        var entity = await GetByIdTrackedAsync(id, cancellationToken);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = CRUDOrders.Application.Common.DateTimeHelper.UtcPlus7Now;
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
    }

    protected virtual IQueryable<T> ApplyKeywordFilter(IQueryable<T> query, string keyword)
    {
        return query; // Override in derived classes
    }
}

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Product>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == code && !p.IsDeleted, cancellationToken);
    }

    protected override IQueryable<Product> ApplyKeywordFilter(IQueryable<Product> query, string keyword)
    {
        return query.Where(p => p.Name.Contains(keyword) || p.Code.Contains(keyword));
    }
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await Context.Set<User>()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await Context.Set<User>()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);
    }

    protected override IQueryable<User> ApplyKeywordFilter(IQueryable<User> query, string keyword)
    {
        return query.Where(u => u.Username.Contains(keyword) || u.Email.Contains(keyword));
    }
}

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByIdWithItemsAsync(long id, CancellationToken cancellationToken = default);
    Task<(List<Order> Items, int Total)> GetUserOrdersAsync(long userId, int skip, int take, CancellationToken cancellationToken = default);
}

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Order?> GetByIdWithItemsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Order>()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);
    }

    public async Task<(List<Order> Items, int Total)> GetUserOrdersAsync(
        long userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Order>()
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Where(o => o.UserId == userId && !o.IsDeleted)
            .OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    protected override IQueryable<Order> ApplyKeywordFilter(IQueryable<Order> query, string keyword)
    {
        return query.Where(o => o.Status.Contains(keyword));
    }
}
