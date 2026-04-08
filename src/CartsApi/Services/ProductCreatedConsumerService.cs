using System.Text;
using System.Text.Json;
using CartsApi.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CartsApi.Services;

public sealed class ProductCreatedConsumerService : BackgroundService
{
    private readonly ILogger<ProductCreatedConsumerService> _logger;
    private readonly ProductCatalogStore _catalog;
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;
    private IModel? _channel;

    public ProductCreatedConsumerService(
        ILogger<ProductCreatedConsumerService> logger,
        ProductCatalogStore catalog,
        IOptions<RabbitMqOptions> options)
    {
        _logger = logger;
        _catalog = catalog;
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
        _channel.QueueDeclare(_options.ProductQueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_options.ProductQueueName, _options.ExchangeName, _options.ProductCreatedRoutingKey);
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
            var @event = JsonSerializer.Deserialize<ProductCreatedEvent>(json);

            if (@event is null)
            {
                _logger.LogWarning("Invalid product.created payload: {Payload}", json);
                _channel.BasicNack(ea.DeliveryTag, false, false);
                return;
            }

            _catalog.Upsert(@event);
            _logger.LogInformation("Product {ProductId} cached for cart validation", @event.ProductId);
            _channel.BasicAck(ea.DeliveryTag, false);
        };

        _channel.BasicConsume(_options.ProductQueueName, false, consumer);
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
