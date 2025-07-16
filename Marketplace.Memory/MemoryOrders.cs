using Staticsoft.Marketplace.Abstractions;
using System.Collections.Concurrent;

namespace Staticsoft.Marketplace.Memory;

public class MemoryOrders : Orders
{
    readonly ConcurrentDictionary<string, Order> orders = new();
    long nextId = 1;
    long nextOrderId = 1001;

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
        var orderId = $"{Interlocked.Increment(ref nextId)}";
        var now = DateTime.UtcNow;

        var order = new Order
        {
            Id = orderId,
            Number = $"{Interlocked.Increment(ref nextOrderId)}",
            CreatedAt = now,
            UpdatedAt = now,
            Status = newOrder.Status,
            TotalPrice = newOrder.Items.Sum(item => item.Price * item.Quantity) + newOrder.TaxAmount,
            SubtotalPrice = newOrder.Items.Sum(item => item.Price * item.Quantity),
            TaxAmount = newOrder.TaxAmount,
            Currency = newOrder.Currency,
            CustomerEmail = newOrder.CustomerEmail,
            Items = newOrder.Items
                .Select(item => new Order.Item
                {
                    Title = item.Title,
                    Quantity = item.Quantity,
                    Price = item.Price
                })
                .ToArray()
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
