using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Messaging
{
    public class RabbitMqEventBus : IEventBus, IDisposable
    {
        private IConnection _connection;

        private static readonly ResiliencePipeline _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<BrokerUnreachableException>()
                    .Handle<Exception>(),
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(3),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            })
            .Build();

        public RabbitMqEventBus(RabbitMqSettings connectionSettings)
        {
            var connFactory = new ConnectionFactory()
            {
                HostName = connectionSettings.HostName,
                UserName = connectionSettings.UserName,
                Password = connectionSettings.Password,
                Port = connectionSettings.Port
            };

            _retryPipeline.Execute(() =>
            {
                _connection = connFactory.CreateConnectionAsync().GetAwaiter().GetResult();
            });
        }

        public async Task Publish<TEvent>(TEvent @event) where TEvent : class
        {
            using var channel = await _connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(
                exchange: "order.exchange",
                type: ExchangeType.Direct,
                durable: true);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event));

            await channel.BasicPublishAsync(
                exchange: "order.exchange",
                routingKey: typeof(TEvent).Name,
                body: body);
        }

        public void Dispose() => _connection?.Dispose();

    }
}