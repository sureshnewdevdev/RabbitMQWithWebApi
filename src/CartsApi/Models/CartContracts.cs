namespace CartsApi.Models;

public sealed record ProductSnapshot(Guid ProductId, string Name, decimal Price, DateTimeOffset CreatedAtUtc);

public sealed record AddCartItemRequest(Guid ProductId, int Quantity);

public sealed record CartItem(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed record Cart(Guid CartId, IReadOnlyList<CartItem> Items, decimal TotalAmount, DateTimeOffset UpdatedAtUtc);

public sealed record CheckoutCartRequest(string CardHolder, string CardNumber, string Currency);

public sealed record CheckoutEvent(Guid PaymentId, Guid CartId, decimal Amount, string Currency, string CardHolder, DateTimeOffset RequestedAtUtc);

public sealed record ProductCreatedEvent(Guid ProductId, string Name, decimal Price, DateTimeOffset CreatedAtUtc);
