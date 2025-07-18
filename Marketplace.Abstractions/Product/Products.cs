namespace Staticsoft.Marketplace.Abstractions;

public interface Products
{
    Task<IReadOnlyCollection<Product>> List();
    Task<Product> Create(NewProduct newProduct);
    Task<Product> Get(string productId);
    Task Delete(string productId);

    public class NotFoundException(
        string productId
    ) : Exception(ToMessage(productId))
    {
        static string ToMessage(string productId)
            => $"Product with ID '{productId}' not found.";
    }
}
