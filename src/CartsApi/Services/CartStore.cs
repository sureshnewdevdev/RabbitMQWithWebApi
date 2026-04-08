using CartsApi.Models;

namespace CartsApi.Services;

public sealed class CartStore
{
    private static readonly Dictionary<Guid, List<CartItem>> Carts = [];
    private static readonly Lock Sync = new();

    public Cart AddItem(Guid cartId, ProductSnapshot product, int quantity)
    {
        lock (Sync)
        {
            if (!Carts.TryGetValue(cartId, out var items))
            {
                items = [];
                Carts[cartId] = items;
            }

            var existing = items.FirstOrDefault(i => i.ProductId == product.ProductId);
            if (existing is null)
            {
                items.Add(new CartItem(product.ProductId, product.Name, product.Price, quantity));
            }
            else
            {
                items.Remove(existing);
                items.Add(existing with { Quantity = existing.Quantity + quantity });
            }

            return BuildCart(cartId, items);
        }
    }

    public Cart? Get(Guid cartId)
    {
        lock (Sync)
        {
            return Carts.TryGetValue(cartId, out var items)
                ? BuildCart(cartId, items)
                : null;
        }
    }

    public Cart? Clear(Guid cartId)
    {
        lock (Sync)
        {
            if (!Carts.Remove(cartId, out var removedItems))
            {
                return null;
            }

            return BuildCart(cartId, removedItems);
        }
    }

    private static Cart BuildCart(Guid cartId, List<CartItem> source)
    {
        var items = source
            .OrderBy(i => i.ProductName)
            .ToList();

        var total = items.Sum(i => i.LineTotal);
        return new Cart(cartId, items, total, DateTimeOffset.UtcNow);
    }
}
