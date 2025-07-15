using Microsoft.Extensions.DependencyInjection;
using Staticsoft.Marketplace.Memory;

namespace Staticsoft.Marketplace.Tests;

public class MemoryOrdersTests : OrdersTests
{
    protected override IServiceCollection Services
        => base.Services
            .UseMemoryShop();
}
