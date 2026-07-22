using Domain.Entities;
using Infrastructure.DBContext;
using Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WebAPI.BackgroundServices;

namespace IntegrationTests.BackgroundServices
{
    public class OutboxProcessorBackgroundServiceTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly OrderServiceDbContext _context;
        private readonly Mock<IEventBus> _eventBusMock;

        public OutboxProcessorBackgroundServiceTests()
        {
            _eventBusMock = new Mock<IEventBus>();

            var options = new DbContextOptionsBuilder<OrderServiceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new OrderServiceDbContext(options);

            var services = new ServiceCollection();
            services.AddSingleton(_context);
            services.AddSingleton<IEventBus>(_eventBusMock.Object);

            _serviceProvider = services.BuildServiceProvider();
        }

        public void Dispose()
        {
            _context.Dispose();
            _serviceProvider.Dispose();
        }

        [Fact]
        public async Task ExecuteAsync_PendingMessage_ShouldPublishAndMarkAsPublished()
        {
            var message = SeedOutboxMessage();
            await _context.SaveChangesAsync();

            await RunProcessorOnce();

            Assert.Equal(OutboxStatus.Published, message.Status);
            Assert.NotNull(message.ProcessedAt);
            _eventBusMock.Verify(e => e.Publish(It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_MessageWithInvalidType_ShouldMarkAsFailed()
        {
            var message = new OutboxMessage("InvalidType.That.DoesNotExist", "{}")
            {
                Status = OutboxStatus.Pending
            };
            _context.OutboxMessages.Add(message);
            await _context.SaveChangesAsync();

            await RunProcessorOnce();

            Assert.Equal(OutboxStatus.Failed, message.Status);
            _eventBusMock.Verify(e => e.Publish(It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_PublishFails_ShouldIncrementRetryCount()
        {
            _eventBusMock
                .Setup(e => e.Publish(It.IsAny<object>()))
                .ThrowsAsync(new Exception("RabbitMQ unavailable"));

            var message = SeedOutboxMessage();
            await _context.SaveChangesAsync();

            await RunProcessorOnce();

            Assert.Equal(1, message.RetryCount);
            Assert.Equal(OutboxStatus.Pending, message.Status);
            Assert.NotNull(message.NextRetryAt);
            Assert.NotNull(message.Error);
        }

        [Fact]
        public async Task ExecuteAsync_PublishFailsThreeTimes_ShouldMarkAsFailed()
        {
            _eventBusMock
                .Setup(e => e.Publish(It.IsAny<object>()))
                .ThrowsAsync(new Exception("RabbitMQ unavailable"));

            var message = SeedOutboxMessage();
            message.RetryCount = 2; // already failed twice
            await _context.SaveChangesAsync();

            await RunProcessorOnce();

            Assert.Equal(OutboxStatus.Failed, message.Status);
            Assert.Equal(3, message.RetryCount);
        }

        [Fact]
        public async Task ExecuteAsync_MessageWithFutureNextRetryAt_ShouldNotProcess()
        {
            var message = SeedOutboxMessage();
            message.NextRetryAt = DateTime.UtcNow.AddMinutes(5);
            await _context.SaveChangesAsync();

            await RunProcessorOnce();

            Assert.Equal(OutboxStatus.Pending, message.Status);
            _eventBusMock.Verify(e => e.Publish(It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_AlreadyPublishedMessage_ShouldNotProcess()
        {
            var message = SeedOutboxMessage();
            message.Status = OutboxStatus.Published;
            await _context.SaveChangesAsync();

            await RunProcessorOnce();

            _eventBusMock.Verify(e => e.Publish(It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_MultipleMessages_ShouldPublishAll()
        {
            SeedOutboxMessage();
            SeedOutboxMessage();
            SeedOutboxMessage();
            await _context.SaveChangesAsync();

            await RunProcessorOnce();

            _eventBusMock.Verify(e => e.Publish(It.IsAny<object>()), Times.Exactly(3));
        }

        // --- Helpers ---

        private OutboxMessage SeedOutboxMessage()
        {
            var @event = new { OrderId = Guid.NewGuid(), CustomerId = Guid.NewGuid() };
            var payload = System.Text.Json.JsonSerializer.Serialize(@event);
            var message = new OutboxMessage(
                type: typeof(object).AssemblyQualifiedName!,
                payload: payload)
            {
                Status = OutboxStatus.Pending
            };
            _context.OutboxMessages.Add(message);
            return message;
        }

        private async Task RunProcessorOnce()
        {
            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var service = new OutboxProcessorBackgroundService(scopeFactory);

            await service.ProcessOutboxMessagesAsync();
        }
    }
}