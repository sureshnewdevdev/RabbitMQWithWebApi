namespace PaymentsApi.Models;

public sealed record CheckoutEvent(Guid PaymentId, Guid CartId, decimal Amount, string Currency, string CardHolder, DateTimeOffset RequestedAtUtc);

public sealed record PaymentRecord(Guid PaymentId, Guid CartId, decimal Amount, string Currency, string CardHolder, DateTimeOffset RequestedAtUtc, string Status, DateTimeOffset ProcessedAtUtc);
