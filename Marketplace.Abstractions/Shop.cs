namespace Staticsoft.Marketplace.Abstractions;

public class Shop(
    Orders orders
)
{
    public Orders Orders { get; } = orders;
}
