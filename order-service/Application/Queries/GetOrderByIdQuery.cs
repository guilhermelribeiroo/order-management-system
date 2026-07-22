using Domain.Entities;
using MediatR;

namespace Application.Queries
{
    public record GetOrderByIdQuery(Guid OrderId) : IRequest<Order?>;
}