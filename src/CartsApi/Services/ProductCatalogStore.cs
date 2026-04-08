using CartsApi.Models;

namespace CartsApi.Services;

public sealed class ProductCatalogStore
{
    private static readonly Dictionary<Guid, ProductSnapshot> Catalog = [];
    private static readonly Lock Sync = new();

    public void Upsert(ProductCreatedEvent @event)
    {
        var snapshot = new ProductSnapshot(@event.ProductId, @event.Name, @event.Price, @event.CreatedAtUtc);
        lock (Sync)
        {
            Catalog[@event.ProductId] = snapshot;
        }
    }

    public bool TryGet(Guid productId, out ProductSnapshot? snapshot)
    {
        lock (Sync)
        {
            return Catalog.TryGetValue(productId, out snapshot);
        }
    }

    public IReadOnlyList<ProductSnapshot> GetAll()
    {
        lock (Sync)
        {
            return Catalog.Values.ToList();
        }
    }
}
