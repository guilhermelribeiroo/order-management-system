using Domain.Entities;
using Infrastructure.DBContext;
using Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.BackgroundServices
{
    public class OutboxProcessorBackgroundService
        : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public OutboxProcessorBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOutboxMessagesAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when the service is shutting down
                    break;
                }
            }
        }

        public async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken = default)
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<OrderServiceDbContext>();

            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

            var pendingMessages = await context.OutboxMessages
                .Where(x => 
                    x.Status == OutboxStatus.Pending
                    && 
                    (
                        x.NextRetryAt == null
                        || 
                        x.NextRetryAt <= DateTime.UtcNow
                    )
                )
                .OrderBy(x => x.CreatedAt)
                .Take(20)
                .ToListAsync(stoppingToken);

            foreach(var message in pendingMessages)
            {
                try
                {
                    var type = Type.GetType(message.Type);

                    if(type is null)
                    {
                        message.Status = OutboxStatus.Failed;
                        continue;
                    }

                    var @event = System.Text.Json.JsonSerializer
                        .Deserialize(message.Payload, type);

                    if(@event is null)
                    {
                        message.Status = OutboxStatus.Failed;
                        continue;
                    }

                    await eventBus.Publish(@event);
                    message.Status = OutboxStatus.Published;
                    message.ProcessedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    message.RetryCount++;
                    message.Error = ex.Message;

                    if(message.RetryCount >= 3)
                    {
                        message.Status = OutboxStatus.Failed;
                    }
                    else
                    {
                        message.Status = OutboxStatus.Pending;

                        message.NextRetryAt = DateTime.UtcNow
                            .AddSeconds(60);
                    }

                }
            }

            await context.SaveChangesAsync(stoppingToken);
        }
    }
}
