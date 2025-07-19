namespace Staticsoft.Marketplace.Abstractions;

public class Shop(
    Orders orders,
    Products products
)
{
    public Orders Orders { get; } = orders;
    public Products Products { get; } = products;
}
