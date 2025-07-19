using Microsoft.Extensions.DependencyInjection;
using Staticsoft.Marketplace.Abstractions;
using Staticsoft.Marketplace.Shopify;

namespace Staticsoft.Marketplace.Tests;

public class ShopifyOrdersTests : OrdersTests
{
    protected override IServiceCollection Services
        => base.Services.UseShopifyServices();
}

public class ShopifyProductsTests : ProductsTests
{
    protected override IServiceCollection Services
        => base.Services.UseShopifyServices();
}

static class ShopifyTestsExtensions
{
    public static IServiceCollection UseShopifyServices(this IServiceCollection services)
        => services
            .UseShopify(_ => new()
            {
                AccessToken = Configuration("ShopifyAccessToken"),
                ShopDomain = Configuration("ShopifyShopDomain")
            })
            .Decorate<Orders, TestOrders>()
            .Decorate<Products, TestProducts>();

    static string Configuration(string name)
        => Environment.GetEnvironmentVariable(name)
        ?? throw new ArgumentException($"Environment variable '{name}' is not set");
}

public class TestOrders(
    Orders orders
) : Orders
{
    readonly Orders Orders = orders;

    public async Task<IReadOnlyCollection<Order>> List()
    {
        var orders = await Orders.List();
        return orders
            .Where(order => order.CustomerEmail == OrdersTests.TestOrderEmail)
            .ToArray()
            .AsReadOnly();
    }

    public Task<Order> Get(string orderId)
        => Orders.Get(orderId);

    public Task<Order> Create(NewOrder newOrder)
        => Orders.Create(newOrder);

    public Task Delete(string orderId)
        => Orders.Delete(orderId);
}

public class TestProducts(
    Products products
) : Products
{
    readonly Products Products = products;

    public async Task<IReadOnlyCollection<Product>> List()
    {
        var products = await Products.List();
        return products
            .Where(product =>
                product.Title.Contains(ProductsTests.TestProductTitle) &&
                product.Description.Contains(ProductsTests.TestProductDescription)
            )
            .ToArray()
            .AsReadOnly();
    }

    public Task<Product> Get(string productId)
        => Products.Get(productId);

    public Task<Product> Create(NewProduct newProduct)
        => Products.Create(newProduct);

    public Task Delete(string productId)
        => Products.Delete(productId);
}
