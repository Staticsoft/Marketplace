using Staticsoft.Marketplace.Abstractions;
using System.Collections.Concurrent;

namespace Staticsoft.Marketplace.Memory;

public class MemoryOrders : Orders
{
    readonly ConcurrentDictionary<string, Order> orders = new();
    long nextId = 1;

    public Task<IReadOnlyCollection<Order>> List()
    {
        var orderList = orders.Values.ToList().AsReadOnly();
        return Task.FromResult<IReadOnlyCollection<Order>>(orderList);
    }

    public Task<Order> Get(string orderId)
    {
        if (!orders.TryGetValue(orderId, out var order))
        {
            throw new Orders.NotFoundException(orderId);
        }

        return Task.FromResult(order);
    }

    public Task<Order> Create(NewOrder newOrder)
    {
        var orderId = Interlocked.Increment(ref nextId).ToString();
        var now = DateTime.UtcNow;

        var order = new Order
        {
            Id = orderId,
            Number = newOrder.Number,
            CreatedAt = now,
            UpdatedAt = now,
            Status = newOrder.Status,
            TotalPrice = newOrder.TotalPrice,
            SubtotalPrice = newOrder.SubtotalPrice,
            TaxAmount = newOrder.TaxAmount,
            Currency = newOrder.Currency,
            CustomerEmail = newOrder.CustomerEmail
        };

        orders.TryAdd(orderId, order);
        return Task.FromResult(order);
    }

    public Task Delete(string orderId)
    {
        if (!orders.TryRemove(orderId, out _))
        {
            throw new Orders.NotFoundException(orderId);
        }

        return Task.CompletedTask;
    }
}
