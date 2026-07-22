using Application.Commands;
using Application.Events;
using Domain.Entities;
using Infrastructure.Interfaces;
using Moq;

namespace UnitTests.Application
{
    public class CreateOrderCommandHandlerTests
    {
        private readonly Mock<IOrderRepository> _repositoryMock;
        private readonly Mock<IOutboxMessageRepository> _outboxMessageRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly CreateOrderCommandHandler _handler;

        public CreateOrderCommandHandlerTests()
        {
            _repositoryMock = new Mock<IOrderRepository>();
            _outboxMessageRepositoryMock = new Mock<IOutboxMessageRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _handler = new CreateOrderCommandHandler(
                _repositoryMock.Object,
                _outboxMessageRepositoryMock.Object,
                _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldAddOrderToRepository()
        {
            var command = BuildValidCommand();

            await _handler.Handle(command, CancellationToken.None);

            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldSaveChanges()
        {
            var command = BuildValidCommand();

            await _handler.Handle(command, CancellationToken.None);

            _unitOfWorkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldAddOutboxMessage()
        {
            var command = BuildValidCommand();

            await _handler.Handle(command, CancellationToken.None);

            _outboxMessageRepositoryMock.Verify(r => r.AddAsync(It.IsAny<OutboxMessage>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldAddOutboxMessageWithCorrectType()
        {
            var command = BuildValidCommand();
            OutboxMessage? capturedMessage = null;

            _outboxMessageRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<OutboxMessage>()))
                .Callback<OutboxMessage>(m => capturedMessage = m)
                .Returns(Task.CompletedTask);

            await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(capturedMessage);
            Assert.Equal(typeof(OrderCreatedEvent).AssemblyQualifiedName, capturedMessage.Type);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldAddOutboxMessageWithCorrectPayload()
        {
            var command = BuildValidCommand();
            OutboxMessage? capturedMessage = null;

            _outboxMessageRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<OutboxMessage>()))
                .Callback<OutboxMessage>(m => capturedMessage = m)
                .Returns(Task.CompletedTask);

            await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(capturedMessage);
            var payload = System.Text.Json.JsonSerializer
                .Deserialize<OrderCreatedEvent>(capturedMessage.Payload);
            Assert.NotNull(payload);
            Assert.Equal(command.CustomerId, payload.CustomerId);
        }

        [Fact]
        public async Task Handle_MultipleItems_ShouldCalculateTotalAmountCorrectly()
        {
            var command = new CreateOrderCommand
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderCommand.OrderItemDto>
                {
                    new() { ProductId = Guid.NewGuid(), ProductName = "A", UnitPrice = 10m, Quantity = 2 },
                    new() { ProductId = Guid.NewGuid(), ProductName = "B", UnitPrice = 5m,  Quantity = 4 }
                }
            };

            OutboxMessage? capturedMessage = null;
            _outboxMessageRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<OutboxMessage>()))
                .Callback<OutboxMessage>(m => capturedMessage = m)
                .Returns(Task.CompletedTask);

            await _handler.Handle(command, CancellationToken.None);

            var payload = System.Text.Json.JsonSerializer
                .Deserialize<OrderCreatedEvent>(capturedMessage!.Payload);
            Assert.Equal(40m, payload!.TotalAmount); // 2*10 + 4*5
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldReturnOrderId()
        {
            var command = BuildValidCommand();

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotEqual(Guid.Empty, result);
        }

        [Fact]
        public async Task Handle_ShouldCallSaveChangesAfterAddingOrderAndOutboxMessage()
        {
            var callOrder = new List<string>();

            _repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Order>()))
                .Callback(() => callOrder.Add("AddOrder"))
                .Returns(Task.CompletedTask);
            _outboxMessageRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<OutboxMessage>()))
                .Callback(() => callOrder.Add("AddOutbox"))
                .Returns(Task.CompletedTask);
            _unitOfWorkMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => callOrder.Add("SaveChangesAsync"))
                .ReturnsAsync(1);

            await _handler.Handle(BuildValidCommand(), CancellationToken.None);

            Assert.Equal(new[] { "AddOrder", "AddOutbox", "SaveChangesAsync" }, callOrder);
        }

        // --- Helpers ---

        private static CreateOrderCommand BuildValidCommand() => new()
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderCommand.OrderItemDto>
            {
                new()
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Test Product",
                    UnitPrice = 25.00m,
                    Quantity = 2
                }
            }
        };
    }
}