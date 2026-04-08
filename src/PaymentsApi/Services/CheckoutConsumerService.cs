using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PaymentsApi.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentsApi.Services;

public sealed class CheckoutConsumerService : BackgroundService
{
    private readonly ILogger<CheckoutConsumerService> _logger;
    private readonly PaymentStore _store;
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;
    private IModel? _channel;

    public CheckoutConsumerService(
        ILogger<CheckoutConsumerService> logger,
        PaymentStore store,
        IOptions<RabbitMqOptions> options)
    {
        _logger = logger;
        _store = store;
        _options = options.Value;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Direct, durable: true);
        _channel.QueueDeclare(_options.PaymentQueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_options.PaymentQueueName, _options.ExchangeName, _options.CartCheckoutRoutingKey);
        _channel.BasicQos(0, 1, false);

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is null)
        {
            throw new InvalidOperationException("RabbitMQ channel has not been initialized.");
        }

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var checkout = JsonSerializer.Deserialize<CheckoutEvent>(json);

            if (checkout is null)
            {
                _logger.LogWarning("Invalid cart.checkout payload: {Payload}", json);
                _channel.BasicNack(ea.DeliveryTag, false, false);
                return;
            }

            var record = new PaymentRecord(
                checkout.PaymentId,
                checkout.CartId,
                checkout.Amount,
                checkout.Currency,
                checkout.CardHolder,
                checkout.RequestedAtUtc,
                "Approved",
                DateTimeOffset.UtcNow);

            _store.Add(record);
            _logger.LogInformation("Payment approved for cart {CartId} with payment id {PaymentId}", checkout.CartId, checkout.PaymentId);
            _channel.BasicAck(ea.DeliveryTag, false);
        };

        _channel.BasicConsume(_options.PaymentQueueName, false, consumer);
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
