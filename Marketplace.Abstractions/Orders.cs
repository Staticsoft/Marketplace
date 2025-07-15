namespace Staticsoft.Marketplace.Abstractions;

public interface Orders
{
    Task<IReadOnlyCollection<Order>> List();
    Task<Order> Create(NewOrder newOrder);
    Task Delete(string orderId);
}
