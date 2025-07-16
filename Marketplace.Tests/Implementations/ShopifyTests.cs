using Microsoft.Extensions.DependencyInjection;
using Staticsoft.Marketplace.Abstractions;
using Staticsoft.Marketplace.Shopify;

namespace Staticsoft.Marketplace.Tests;

public class ShopifyOrdersTests : OrdersTests
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
            .Decorate<Orders, TestOrders>();

    static string Configuration(string name)
        => Environment.GetEnvironmentVariable(name)
        ?? throw new ArgumentException($"Environment variable '{name}' is not set");
}

public class TestOrders(
    Orders orders
) : Orders
{
    readonly Orders Orders = orders;

    public Task<IReadOnlyCollection<Order>> List()
        => Orders.List();

    public Task<Order> Get(string orderId)
        => Orders.Get(orderId);

    public Task<Order> Create(NewOrder newOrder)
        => Orders.Create(newOrder);

    public Task Delete(string orderId)
        => Orders.Delete(orderId);
}