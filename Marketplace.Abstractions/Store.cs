namespace Staticsoft.Marketplace.Abstractions;

public class Store(
    Orders orders
)
{
    public Orders Orders { get; } = orders;
}
