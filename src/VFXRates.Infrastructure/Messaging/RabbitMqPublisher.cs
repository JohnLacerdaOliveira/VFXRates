using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using VFXRates.Application.Interfaces;
using VFXRates.Domain.Entities;
using IModel = RabbitMQ.Client.IModel;

public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly string _exchange;

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"],
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? string.Empty),
            UserName = configuration["RabbitMQ:UserName"],
            Password = configuration["RabbitMQ:Password"]
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _exchange = configuration["RabbitMQ:Exchange"] ?? string.Empty;
        _channel.ExchangeDeclare(exchange: _exchange, type: ExchangeType.Fanout);

        var queueName = configuration["RabbitMQ:Queue"] ?? "fxrates_queue";
        _channel.QueueDeclare(queue: queueName,
                              durable: true,
                              exclusive: false,
                              autoDelete: false,
                              arguments: null);

        _channel.QueueBind(queue: queueName,
                           exchange: _exchange,
                           routingKey: "");

        _logger.LogInformation("RabbitMQ publisher initialized with exchange: {Exchange}", _exchange);
    }

    public Task PublishFxRateCreation(FxRate newRate)
    {
        try
        {
            var message = JsonSerializer.Serialize(newRate);
            var body = Encoding.UTF8.GetBytes(message);

            _channel.BasicPublish(
                exchange: _exchange,
                routingKey: "",
                basicProperties: null,
                body: body
            );

            _logger.LogInformation("Published new FX rate event for {BaseCurrency}/{QuoteCurrency}.", newRate.BaseCurrency, newRate.QuoteCurrency);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish new FX rate event for {BaseCurrency}/{QuoteCurrency}.", newRate.BaseCurrency, newRate.QuoteCurrency);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _logger.LogInformation("RabbitMQ connection and channel closed.");
    }
}
