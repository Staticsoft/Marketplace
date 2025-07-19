using Staticsoft.Marketplace.Abstractions;
using System.Collections.Concurrent;

namespace Staticsoft.Marketplace.Memory;

public class MemoryProducts : Products
{
    readonly ConcurrentDictionary<string, Product> products = new();
    long nextId = 1;
    long nextVariationId = 1;

    public Task<IReadOnlyCollection<Product>> List()
    {
        var productList = products.Values.ToList().AsReadOnly();
        return Task.FromResult<IReadOnlyCollection<Product>>(productList);
    }

    public Task<Product> Get(string productId)
    {
        if (!products.TryGetValue(productId, out var product))
        {
            throw new Products.NotFoundException(productId);
        }

        return Task.FromResult(product);
    }

    public Task<Product> Create(NewProduct newProduct)
    {
        var productId = $"{Interlocked.Increment(ref nextId)}";
        var now = DateTime.UtcNow;

        var product = new Product
        {
            Id = productId,
            Title = newProduct.Title,
            Description = newProduct.Description,
            Status = newProduct.Status,
            CreatedAt = now,
            UpdatedAt = now,
            Variations = newProduct.Variations
                .Select(variation => new Product.Variation
                {
                    Id = $"{Interlocked.Increment(ref nextVariationId)}",
                    Title = variation.Title,
                    Price = variation.Price,
                    InventoryQuantity = variation.InventoryQuantity,
                    CreatedAt = now,
                    UpdatedAt = now
                })
                .ToArray()
        };

        products.TryAdd(productId, product);
        return Task.FromResult(product);
    }

    public Task Delete(string productId)
    {
        if (!products.TryRemove(productId, out _))
        {
            throw new Products.NotFoundException(productId);
        }

        return Task.CompletedTask;
    }
}
