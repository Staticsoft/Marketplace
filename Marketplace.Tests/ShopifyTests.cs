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
            .UseShopify();
}