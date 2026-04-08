using ProductsApi.Models;

namespace ProductsApi.Services;

public sealed class ProductStore
{
    private static readonly List<Product> Products = [];
    private static readonly Lock Sync = new();

    public Product Add(string name, decimal price)
    {
        var product = new Product(Guid.NewGuid(), name.Trim(), price, DateTimeOffset.UtcNow);
        lock (Sync)
        {
            Products.Add(product);
        }

        return product;
    }

    public IReadOnlyList<Product> GetAll()
    {
        lock (Sync)
        {
            return Products.ToList();
        }
    }
}
