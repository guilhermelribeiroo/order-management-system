using Application.Events;
using Domain.Entities;
using Infrastructure.Interfaces;
using Infrastructure.Messaging;
using MediatR;
using System.Text.Json;

namespace Application.Commands
{
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOutboxMessageRepository _outboxMessageRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventBus _eventBus;

        public CreateOrderCommandHandler(IOrderRepository orderRepository, IOutboxMessageRepository outboxMessageRepository, IUnitOfWork unitOfWork, IEventBus eventBus)
        {
            _orderRepository = orderRepository;
            _outboxMessageRepository = outboxMessageRepository;
            _unitOfWork = unitOfWork;
            _eventBus = eventBus;
        }

        public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            var order = new Order(request.CustomerId);

            foreach (var item in request.Items)
            {
                order.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);
            }

            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                TotalAmount = order.TotalAmount,
                CreatedAt = DateTime.UtcNow
            };

            var outboxMessage = new OutboxMessage(
                type: typeof(OrderCreatedEvent).AssemblyQualifiedName!,
                payload: JsonSerializer.Serialize(orderCreatedEvent));

            await _orderRepository.AddAsync(order);
            await _outboxMessageRepository.AddAsync(outboxMessage);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _eventBus.Publish(orderCreatedEvent);

            return order.Id;
        }   
    }
}
