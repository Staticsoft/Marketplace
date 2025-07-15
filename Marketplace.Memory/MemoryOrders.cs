using Staticsoft.Marketplace.Abstractions;

namespace Staticsoft.Marketplace.Memory;

public class MemoryOrders : Orders
{
    public Task<IReadOnlyCollection<Order>> List()
    {
        throw new NotImplementedException();
    }

    public Task<Order> Get(string orderId)
    {
        throw new NotImplementedException();
    }

    public Task<Order> Create(NewOrder newOrder)
    {
        throw new NotImplementedException();
    }

    public Task Delete(string orderId)
    {
        throw new NotImplementedException();
    }
}
