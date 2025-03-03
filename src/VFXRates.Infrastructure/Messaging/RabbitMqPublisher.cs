using Microsoft.Extensions.Configuration;
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
    private readonly ILogService _logService;
    private readonly string _exchange;

    public RabbitMqPublisher(IConfiguration configuration, ILogService logService)
    {
        _logService = logService;

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
    }

    public async Task PublishFxRateCreation(FxRate newRate)
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

            await _logService.LogInformation($"Published new FX rate event for {newRate.BaseCurrency}/{newRate.QuoteCurrency}.");
            
        }
        catch (Exception ex)
        {
            await _logService.LogError($"Failed to publish new FX rate event for {newRate.BaseCurrency}/{newRate.QuoteCurrency}.",ex);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _logService.LogInformation("RabbitMQ connection and channel closed.").GetAwaiter().GetResult();
    }
}
