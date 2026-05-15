using Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebAPI.Controllers;

namespace UnitTests.WebAPI
{
    public class OrdersControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly OrdersController _controller;

        public OrdersControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _controller = new OrdersController(_mediatorMock.Object);
        }

        [Fact]
        public async Task Create_ValidCommand_ShouldReturnOk()
        {
            var command = BuildValidCommand();
            var newOrderId = Guid.NewGuid();
            _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(newOrderId);

            var result = await _controller.Create(command);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Create_ValidCommand_ShouldReturnOrderIdInBody()
        {
            var command = BuildValidCommand();
            var newOrderId = Guid.NewGuid();
            _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(newOrderId);

            var result = await _controller.Create(command) as OkObjectResult;
            var body = result!.Value;

            // The response is an anonymous object { OrderId = ... }
            var orderId = body!.GetType().GetProperty("OrderId")!.GetValue(body);
            Assert.Equal(newOrderId, orderId);
        }

        [Fact]
        public async Task Create_ShouldSendCommandToMediator()
        {
            var command = BuildValidCommand();
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateOrderCommand>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(Guid.NewGuid());

            await _controller.Create(command);

            _mediatorMock.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Create_WhenMediatorThrows_ShouldPropagateException()
        {
            var command = BuildValidCommand();
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateOrderCommand>(), It.IsAny<CancellationToken>()))
                         .ThrowsAsync(new InvalidOperationException("Something went wrong"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Create(command));
        }

        // --- Helpers ---
        // Could be another class if more code is added
        private static CreateOrderCommand BuildValidCommand() => new()
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderCommand.OrderItemDto>
            {
                new()
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Test Product",
                    UnitPrice = 20.00m,
                    Quantity = 1
                }
            }
        };
    }

}
