using System.Collections.Concurrent;
using MinimalEndpoints.AdvancedSample.Models;

namespace MinimalEndpoints.AdvancedSample.Services;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(Product product);
    Task<Product?> UpdateAsync(int id, UpdateProductRequest request);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}

public class InMemoryProductRepository : IProductRepository
{
    // Registered as a singleton, so the backing store must be safe for concurrent requests:
    // a plain List + `_nextId++` race under load. ConcurrentDictionary + Interlocked fix that.
    private readonly ConcurrentDictionary<int, Product> _products = new();
    private int _nextId;

    public InMemoryProductRepository()
    {
        // Seed data
        Seed("Laptop", "High-performance laptop", 999.99m, 10);
        Seed("Mouse", "Wireless mouse", 29.99m, 50);
        Seed("Keyboard", "Mechanical keyboard", 79.99m, 25);
    }

    private void Seed(string name, string description, decimal price, int stock)
    {
        var id = Interlocked.Increment(ref _nextId);
        _products[id] = new Product
        {
            Id = id,
            Name = name,
            Description = description,
            Price = price,
            Stock = stock,
            CreatedAt = DateTime.UtcNow
        };
    }

    public Task<IEnumerable<Product>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<Product>>(_products.Values.OrderBy(p => p.Id).ToList());
    }

    public Task<Product?> GetByIdAsync(int id)
    {
        return Task.FromResult(_products.TryGetValue(id, out var product) ? product : null);
    }

    public Task<Product> CreateAsync(Product product)
    {
        product.Id = Interlocked.Increment(ref _nextId);
        product.CreatedAt = DateTime.UtcNow;
        _products[product.Id] = product;
        return Task.FromResult(product);
    }

    public Task<Product?> UpdateAsync(int id, UpdateProductRequest request)
    {
        if (!_products.TryGetValue(id, out var product))
            return Task.FromResult<Product?>(null);

        if (request.Name != null) product.Name = request.Name;
        if (request.Description != null) product.Description = request.Description;
        if (request.Price.HasValue) product.Price = request.Price.Value;
        if (request.Stock.HasValue) product.Stock = request.Stock.Value;
        product.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult<Product?>(product);
    }

    public Task<bool> DeleteAsync(int id)
    {
        return Task.FromResult(_products.TryRemove(id, out _));
    }

    public Task<bool> ExistsAsync(int id)
    {
        return Task.FromResult(_products.ContainsKey(id));
    }
}

