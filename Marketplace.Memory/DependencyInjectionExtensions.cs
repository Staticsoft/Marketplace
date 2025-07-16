using Microsoft.Extensions.DependencyInjection;
using Staticsoft.Marketplace.Abstractions;

namespace Staticsoft.Marketplace.Memory;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection UseMemoryShop(this IServiceCollection services)
        => services
            .AddSingleton<Shop>()
            .AddSingleton<Orders, MemoryOrders>();
}
