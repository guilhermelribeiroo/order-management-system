using Domain.Entities;

namespace Infrastructure.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> GetOrderWithItemsAsync(Guid orderId);
    }
}