using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Messaging
{
    public class RabbitMqEventBus : IEventBus
    {
        private IConnection _connection;

        public RabbitMqEventBus(RabbitMqSettings connectionSettings)
        {
            var connFactory = new ConnectionFactory()
            {
                HostName = connectionSettings.HostName,
                UserName = connectionSettings.UserName,
                Password = connectionSettings.Password,
                Port = connectionSettings.Port
            };

            _connection = connFactory.CreateConnectionAsync().GetAwaiter().GetResult();
        }

        public async Task Publish<TEvent>(TEvent @event) where TEvent : class
        {
            using var channel = await _connection.CreateChannelAsync();
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event));
            await channel.BasicPublishAsync(exchange: "",
                                 routingKey: typeof(TEvent).Name,
                                 body: body);
        }
    }
}
