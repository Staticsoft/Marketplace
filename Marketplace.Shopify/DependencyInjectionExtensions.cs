using Microsoft.Extensions.DependencyInjection;
using ShopifySharp;
using ShopifySharp.Extensions.DependencyInjection;
using Staticsoft.Marketplace.Abstractions;

namespace Staticsoft.Marketplace.Shopify;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection UseShopify(this IServiceCollection services) => services
        .AddShopifySharp<LeakyBucketExecutionPolicy>()
        .AddScoped<Orders, ShopifyOrders>();
}
