namespace Staticsoft.Marketplace.Abstractions;

public interface Orders
{
    Task<IReadOnlyCollection<Order>> List();
    Task<Order> Create(NewOrder newOrder);
    Task<Order> Get(string orderId);
    Task Delete(string orderId);
}
