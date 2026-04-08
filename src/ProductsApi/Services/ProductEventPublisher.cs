using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProductsApi.Models;
using RabbitMQ.Client;

namespace ProductsApi.Services;

public sealed class ProductEventPublisher : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly RabbitMqOptions _options;

    public ProductEventPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;

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
    }

    public void PublishProductCreated(Product product)
    {
        var @event = new ProductCreatedEvent(product.Id, product.Name, product.Price, product.CreatedAtUtc);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event));

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        _channel.BasicPublish(_options.ExchangeName, _options.ProductCreatedRoutingKey, properties, body);
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
