using Application.Commands;
using Application.Events;
using Domain.Entities;
using Infrastructure.Interfaces;
using Infrastructure.Messaging;
using Moq;

namespace UnitTests.Application
{
    public class CreateOrderCommandHandlerTests
    {
        private readonly Mock<IOrderRepository> _repositoryMock;
        private readonly Mock<IOutboxMessageRepository> _outboxMessageRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IEventBus> _eventBusMock;
        private readonly CreateOrderCommandHandler _handler;

        public CreateOrderCommandHandlerTests()
        {
            _repositoryMock = new Mock<IOrderRepository>();
            _eventBusMock = new Mock<IEventBus>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _outboxMessageRepositoryMock = new Mock<IOutboxMessageRepository>();
            _handler = new CreateOrderCommandHandler(_repositoryMock.Object, _outboxMessageRepositoryMock.Object, _unitOfWorkMock.Object, _eventBusMock.Object);
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

            _unitOfWorkMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldPublishOrderCreatedEvent()
        {
            var command = BuildValidCommand();

            await _handler.Handle(command, CancellationToken.None);

            _eventBusMock.Verify(
                e => e.Publish(It.IsAny<OrderCreatedEvent>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldPublishEventWithCorrectData()
        {
            var command = BuildValidCommand();
            OrderCreatedEvent? capturedEvent = null;

            _eventBusMock
                .Setup(e => e.Publish(It.IsAny<OrderCreatedEvent>()))
                .Callback<OrderCreatedEvent>(e => capturedEvent = e)
                .Returns(Task.CompletedTask);

            await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(capturedEvent);
            Assert.Equal(command.CustomerId, capturedEvent.CustomerId);
            Assert.True(capturedEvent.TotalAmount > 0);
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

            OrderCreatedEvent? capturedEvent = null;
            _eventBusMock
                .Setup(e => e.Publish(It.IsAny<OrderCreatedEvent>()))
                .Callback<OrderCreatedEvent>(e => capturedEvent = e)
                .Returns(Task.CompletedTask);

            await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(40m, capturedEvent!.TotalAmount); // 2*10 + 4*5
        }

        [Fact]
        public async Task Handle_ShouldCallSaveChangesAfterAddAsync()
        {
            var callOrder = new List<string>();
            _repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Order>()))
                .Callback(() => callOrder.Add("AddAsync"))
                .Returns(Task.CompletedTask);
            _unitOfWorkMock
                .Setup(r => r.SaveChangesAsync())
                .Callback(() => callOrder.Add("SaveChangesAsync"))
                .ReturnsAsync(1);

            await _handler.Handle(BuildValidCommand(), CancellationToken.None);

            Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
        }

        // Kind of useless tests because the guid is generated in the database, but it can be useful if we want to change the implementation later
        // It also verifies that the handler returns the generated order id, which is a contract of the method
        // not feeling like adding "= Guid.NewGuid()" in the BaseEntity Id property because could cause issues if the entity is created but not saved
        [Fact]
        public async Task Handle_EmptyItemsList_ShouldStillCreateOrderAndPublishEvent()
        {
            var command = new CreateOrderCommand
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderCommand.OrderItemDto>()
            };

            _repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Order>()))
                .Callback<Order>(order => order.Id = Guid.NewGuid())
                .Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotEqual(Guid.Empty, result);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
            _eventBusMock.Verify(e => e.Publish(It.IsAny<OrderCreatedEvent>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldReturnOrderId()
        {
            var command = BuildValidCommand();

            _repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Order>()))
                .Callback<Order>(order => order.Id = Guid.NewGuid())
                .Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotEqual(Guid.Empty, result);
        }

        // --- Helpers ---
        // Could be a class if more code is added

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
