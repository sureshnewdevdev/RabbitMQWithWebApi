namespace PaymentsApi.Services;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string ExchangeName { get; init; } = "commerce.exchange";
    public string CartCheckoutRoutingKey { get; init; } = "cart.checkout";
    public string PaymentQueueName { get; init; } = "payments.checkout.queue";
}
