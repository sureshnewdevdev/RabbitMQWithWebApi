using PaymentsApi.Models;

namespace PaymentsApi.Services;

public sealed class PaymentStore
{
    private static readonly List<PaymentRecord> Payments = [];
    private static readonly Lock Sync = new();

    public void Add(PaymentRecord payment)
    {
        lock (Sync)
        {
            Payments.Add(payment);
        }
    }

    public IReadOnlyList<PaymentRecord> GetAll()
    {
        lock (Sync)
        {
            return Payments
                .OrderByDescending(p => p.ProcessedAtUtc)
                .ToList();
        }
    }
}
