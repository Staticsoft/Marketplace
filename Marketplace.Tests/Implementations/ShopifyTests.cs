using Microsoft.Extensions.DependencyInjection;
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
            });

    static string Configuration(string name)
        => Environment.GetEnvironmentVariable(name)
        ?? throw new ArgumentException($"Environment variable '{name}' is not set");
}